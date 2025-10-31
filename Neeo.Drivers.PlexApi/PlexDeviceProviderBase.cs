using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

public abstract class PlexDeviceProviderBase(
    IHttpClientFactory httpClientFactory,
    IPlexDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    ILogger logger,
    string deviceName,
    DeviceType deviceType
) : IDeviceProvider, IDisposable
{
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");
    private static readonly Uri _userUri = new($"https://plex.tv/api/v2/user");

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly Dictionary<IPlexServer, CancellationTokenSource> _serverCancellationTokens = [];
    private readonly ConcurrentDictionary<string, IPlexServer> _servers = [];
    private string[]? _initialDeviceIds;
    private IDeviceNotifier? _notifier;
    private string _uriPrefix = string.Empty;

    public IDeviceBuilder DeviceBuilder => field ??= this.CreateDevice();

    public void Dispose()
    {
        this._httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    protected static class Components
    {
        public const string Player = "PLAYER";

        public static string GetSensor(string componentName) => $"{componentName}_SENSOR";
    }

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(deviceName, deviceType)
        .AddAdditionalSearchTokens("PMS")
        .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
        .AddButtonHandler(this.HandleButtonAsync)
        .AddPowerStateSensor(this.GetPowerStateAsync)
        .AddTextLabel(Components.Player, "Player: ", (deviceId, _) => Task.FromResult(this.GetSelectedPlayer(deviceId)))
        .AddDirectory("Players", "Players", null, this.BrowsePlayersAsync, this.HandleSelectPlayerAsync)
        .EnableDeviceRoute(this.SetUriPrefix, this.HandleDeviceRouteAsync)
        .EnableNotifications(notifier => this._notifier = notifier)
        .EnableDiscovery("Plex Discovery", "Select Plex Server", this.DiscoverDevicesAsync)
        .EnableRegistration("Plex Registration", "Enter your credentials to connect to Plex", this.QueryIsRegisteredAsync, this.RegisterDeviceAsync)
        .RegisterDeviceSubscriptionCallbacks(this.HandleDeviceAddedAsync, this.HandleDeviceRemovedAsync, this.SetInitialDeviceIdsAsync)
        .RegisterInitializer(this.InitializeAsync)
        .SetManufacturer("Plex");

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing...");
        await discovery.InitializeAsync().ConfigureAwait(false);
        if (Interlocked.Exchange(ref this._initialDeviceIds, null) is not { Length: > 0 } deviceIds)
        {
            return;
        }
        try
        {
            await Parallel.ForEachAsync(
                deviceIds,
                cancellationToken,
                async (deviceId, cancellationToken) => await this.HandleDeviceAddedAsync(deviceId, cancellationToken).ConfigureAwait(false)
            );
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private async Task BrowsePlayersAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        (string serverName, _) = (DeviceId)deviceId;
        if (!this._servers.TryGetValue(serverName, out IPlexServer? server))
        {
            return;
        }
        PlexPlayerInfo[] players = await server.GetPlayersAsync(cancellationToken).ConfigureAwait(false);
        if (players.Length == 0)
        {
            builder.AddEntry(new("Clients not found!", "Click to attempt reloading the list", /* ThumbnailUri: Images.Kodi */ UIAction: DirectoryUIAction.Reload));
            return;
        }
        builder.AddHeader("Available Players");
        foreach (PlexPlayerInfo player in players)
        {
            builder.AddEntry(new(player.Name, /* ThumbnailUri: player.GetThumbnailUri(), */ ActionIdentifier: player.MachineIdentifier, UIAction: DirectoryUIAction.Close));
        }
        builder.SetTotalMatchingItems(players.Length);
    }

    private async Task<DiscoveredDevice[]> DiscoverDevicesAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        if (optionalDeviceId is not { Length: > 0 })
        {
            return [.. discovery.Servers.Select(entry => new DiscoveredDevice(new DeviceId(entry.Key, deviceType), entry.Key))];
        }
        DeviceId deviceId = (DeviceId)optionalDeviceId;
        return deviceId.Type == deviceType && discovery.Servers.ContainsKey(deviceId.ServerName)
            ? [new DiscoveredDevice(deviceId, deviceId.ServerName)]
            : [];
    }

    private async Task<bool> GetPowerStateAsync(string deviceId, CancellationToken cancellationToken)
    {
        (string serverName, _) = (DeviceId)deviceId;
        if (!this._servers.TryGetValue(serverName, out IPlexServer? server))
        {
            return false;
        }
        return false;
    }

    private string GetSelectedPlayer(string deviceId)
    {
        (string serverName, _) = (DeviceId)deviceId;
        return this._servers.TryGetValue(serverName, out IPlexServer? server) && server.SelectedPlayer is { Name: { } name }
            ? name
            : string.Empty;
    }

    private async Task HandleButtonAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        (string serverName, _) = (DeviceId)deviceId;
        if (!this._servers.TryGetValue(serverName, out IPlexServer? server))
        {
            return;
        }
        logger.LogInformation("Plex button pressed: {buttonName} on server: {serverName}", buttonName, server.ServerName);
    }

    private async Task HandleDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        (string serverName, _) = (DeviceId)deviceId;
        if (await serverManager.GetServerAsync(serverName, cancellationToken).ConfigureAwait(false) is not { } server)
        {
            logger.LogWarning("Plex device added but server not found: {ServerName}", serverName);
            return;
        }
        logger.LogInformation("Plex device added: {ServerName} ({HostName})", serverName, server.Info.HostName);
        if (!this._servers.TryAdd(serverName, server))
        {
            return;
        }
        CancellationTokenSource source = new();
        server.SelectedPlayerChanged
            .SelectMany(player => Observable.FromAsync(() => this.OnSelectedPlayerChanged(server, player)))
            .TakeUntil(source.Token)
            .Subscribe(
                onNext: _ => { },
                onError: ex => logger.LogError(ex, "Error in player changed handler")
            );
        this._serverCancellationTokens.Add(server, source);
    }

    private async Task HandleDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        (string serverName, _) = (DeviceId)deviceId;
        if (!this._servers.TryRemove(serverName, out IPlexServer? server))
        {
            return;
        }
        logger.LogInformation("Plex device removed: {serverName}", serverName);
        using CancellationTokenSource source = this._serverCancellationTokens[server];
        source.Cancel();
        server.Dispose();
    }

    private async Task<ActionResult> HandleDeviceRouteAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        return new OkResult();
    }

    private async Task HandleSelectPlayerAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        (string serverName, _) = (DeviceId)deviceId;
        if (this._servers.TryGetValue(serverName, out IPlexServer? server))
        {
            await server.SelectPlayerAsync(actionIdentifier, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnSelectedPlayerChanged(IPlexServer server, PlexPlayerInfo player)
    {
        if (this._notifier is { } notifier)
        {
            await notifier.SendNotificationAsync(Components.GetSensor(Components.Player), player.Name, new DeviceId(server.ServerName, deviceType)).ConfigureAwait(false);
        }
    }

    private async Task<bool> QueryIsRegisteredAsync(CancellationToken cancellationToken)
    {
        // Check if token exists, we have not previously registered if it doesn't.
        if (string.IsNullOrEmpty(tokenStore.AuthToken))
        {
            return false;
        }
        // Check if token is valid.
        if (await TryValidateTokenAsync(cancellationToken).ConfigureAwait(false))
        {
            return true;
        }
        // Clear invalid token.
        tokenStore.AuthToken = null;
        return false;

        async Task<bool> TryValidateTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                HttpStatusCode statusCode = await this._httpClient.HeadAsync(
                    PlexDeviceProviderBase._userUri,
                    request => request.Headers.Add("X-Plex-Token", tokenStore.AuthToken),
                    cancellationToken
                ).ConfigureAwait(false);
                return statusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }
    }

    private async Task<RegistrationResult> RegisterDeviceAsync(string userName, string password, CancellationToken cancellationToken)
    {
        try
        {
            JsonElement element = await this._httpClient.PostAsync<JsonElement>(
                PlexDeviceProviderBase._signInUri,
                configureRequest: ConfigureRequest,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            tokenStore.AuthToken = element.GetProperty("user").GetProperty("authToken").GetString()!;
            return RegistrationResult.Success;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RegistrationResult.Failed("Unauthorized");
        }

        void ConfigureRequest(HttpRequestMessage request)
        {
            request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
            request.Headers.Add("X-Plex-Version", "1");
            request.Headers.Add("X-Plex-Product", Assembly.GetExecutingAssembly().GetName().Name);
            request.Headers.Add("X-Plex-Client-Identifier", tokenStore.ClientIdentifier);
        }
    }

    private Task SetInitialDeviceIdsAsync(string[] deviceIds, CancellationToken cancellationToken)
    {
        logger.LogInformation("Set device list: [{DeviceIds}]", string.Join(',', deviceIds));
        this._initialDeviceIds = deviceIds;
        return Task.CompletedTask;
    }

    private void SetUriPrefix(string prefix) => this._uriPrefix = prefix;
}
