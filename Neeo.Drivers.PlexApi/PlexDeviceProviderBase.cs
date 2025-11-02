using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Server.Drivers;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

public abstract class PlexDeviceProviderBase(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    ILogger logger,
    DeviceType deviceType,
    string deviceName
) : IDeviceProvider, IDisposable
{
    protected static readonly IReadOnlyDictionary<Buttons, string> ButtonCommandMapping = new Dictionary<Buttons, string>(){
        { Buttons.CursorUp, "navigation/moveUp" },
        { Buttons.CursorDown, "navigation/moveDown" },
        { Buttons.CursorLeft, "navigation/moveLeft" },
        { Buttons.CursorRight, "navigation/moveRight" },
        { Buttons.CursorEnter, "navigation/select" },
        { Buttons.Home,"navigation/home" },
        { Buttons.Menu,"navigation/home" },
        { Buttons.Back,"navigation/back" },
        { Buttons.Play ,"playback/play" },
        { Buttons.Pause, "playback/pause" },
        { Buttons.Stop ,"playback/stop" },
        { Buttons.Forward, "playback/stepForward" },
        { Buttons.Reverse, "playback/stepBack" },
        { Buttons.Next, "playback/skipNext" },
        { Buttons.Previous, "playback/skipPrevious" }
    };

    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");
    private static readonly Uri _userUri = new($"https://plex.tv/api/v2/user");

    private readonly EmbeddedImages _embeddedImages = new();
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly ConcurrentDictionary<string, IPlexServer> _servers = [];
    private string[]? _initialServerNames;
    private IDeviceNotifier? _notifier;
    private string _uriPrefix = string.Empty;

    public IDeviceBuilder DeviceBuilder => field ??= this.CreateDevice();

    public void Dispose()
    {
        this._httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    protected Task BrowseRootDirectoryAsync(string serverName, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is not { } server)
        {
            return Task.CompletedTask;
        }
        switch (builder.Parameters.BrowseIdentifier)
        {
            case null or { Length: 0 }:
                builder
                    .AddTileRow(new DirectoryTile(this._embeddedImages.Logo))
                    .AddEntry(new(
                        this.GetSelectedPlayer(serverName) is { } name ? $"Select player ({name})" : "Select player",
                        BrowseIdentifier: "player",
                        ThumbnailUri: this._embeddedImages.Player
                    ));
                break;
            case "player":
                return this.BrowsePlayersAsync(server, builder, cancellationToken);
        }
        return Task.CompletedTask;
    }

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(deviceName, deviceType)
        .AddAdditionalSearchTokens("PMS")
        .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
        .AddButtonHandler(this.HandleButtonAsync)
        .AddPowerStateSensor((serverName, _) => Task.FromResult(this.IsConnected(serverName)))
        .AddTextLabel(Components.Player, "Player:", async (serverName, _) => this.GetSelectedPlayer(serverName) ?? string.Empty)
        .EnableDeviceRoute(this.SetUriPrefix, this.HandleDeviceRouteAsync)
        .EnableDiscovery("Discovering Plex Servers", "Select Plex Server", async (optionalDeviceId, _) => this.GetDiscoveredServers(optionalDeviceId))
        .EnableNotifications(notifier => this._notifier = notifier)
        .EnableRegistration("Plex Registration", "Enter your credentials to connect to Plex", this.QueryIsRegisteredAsync, this.RegisterDeviceAsync)
        .RegisterDeviceSubscriptionCallbacks(this.HandleDeviceAddedAsync, this.HandleDeviceRemovedAsync, async (serverNames, _) => this.SetInitialServerNames(serverNames))
        .RegisterInitializer(this.InitializeAsync)
        .SetManufacturer("Plex");

    protected string EncodeThumbnailUri(string uri)
    {
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(uri));
        return $"{this._uriPrefix}{VirtualDirectories.Thumbnail}/{base64.TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }

    protected Task<string> GetCoverArtAsync(string serverName, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }

    protected Task<string> GetDescriptionAsync(string serverName, CancellationToken cancellation) => Task.FromResult(this.IsConnected(serverName).ToString());

    protected IPlexServer? GetServer(string serverName) => this._servers.GetValueOrDefault(serverName);

    protected Task<string> GetTitleAsync(string serverName, CancellationToken cancellation) => Task.FromResult(this.IsConnected(serverName).ToString());

    protected Task HandleRootDirectoryActionAsync(string serverName, string actionIdentifier, CancellationToken cancellationToken)
    {
        int index = actionIdentifier.IndexOf('.');
        if (index == -1)
        {
            return Task.CompletedTask;
        }
        string type = actionIdentifier[..index];
        string selection = actionIdentifier[(index + 1)..];
        return type switch
        {
            "player" => this.HandleSelectPlayerAsync(serverName, selection, cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    protected bool IsPlaying(string serverName) => this.GetServer(serverName) is { PlayState: PlayState.Playing or PlayState.Buffering };

    private static string GetContentType(string fileName)
    {
        return PlexDeviceProviderBase._contentTypeProvider.TryGetContentType(fileName, out string? contentType)
            ? contentType
            : "application/octet-stream";
    }

    private async Task BrowsePlayersAsync(IPlexServer server, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        PlexPlayerInfo[] players = await server.GetPlayersAsync(cancellationToken).ConfigureAwait(false);
        if (players.Length == 0)
        {
            builder.AddEntry(new("Clients not found!", "Click to attempt reloading the list", /* ThumbnailUri: Images.Kodi */ UIAction: DirectoryUIAction.Reload));
            return;
        }
        builder.AddHeader("Available Players");
        foreach (PlexPlayerInfo player in players)
        {
            builder.AddEntry(new(player.Name, ThumbnailUri: this._embeddedImages.Player, /* ThumbnailUri: player.GetThumbnailUri(), */ ActionIdentifier: $"player.{player.MachineIdentifier}", UIAction: DirectoryUIAction.Close));
        }
        builder.SetTotalMatchingItems(players.Length);
    }

    private DiscoveredDevice[] GetDiscoveredServers(string? optionalDeviceId)
    {
        return optionalDeviceId switch
        {
            { Length: > 0 } id when discovery.Servers.TryGetValue(id, out PlexServerInfo info) => [ServerDiscoveryResult(info)],
            null => [.. discovery.Servers.Values.Select(ServerDiscoveryResult)],
            _ => []
        };

        DiscoveredDevice ServerDiscoveryResult(PlexServerInfo info) => new(info.Name, $"{info.Name} ({deviceName})");
    }

    private string? GetSelectedPlayer(string serverName) => this.GetServer(serverName)?.SelectedPlayer?.Name;

    private async Task HandleButtonAsync(string serverName, string buttonName, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is { } server &&
            Button.TryResolve(buttonName) is { } button &&
            PlexDeviceProviderBase.ButtonCommandMapping.TryGetValue(button, out string? command))
        {
            logger.LogInformation("Plex button pressed: {buttonName} on server: {serverName}", buttonName, server.Name);
            await server.SendPlayerCommandAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleDeviceAddedAsync(string serverName, CancellationToken cancellationToken)
    {
        if (await serverManager.GetServerAsync(serverName, cancellationToken).ConfigureAwait(false) is not { } server)
        {
            logger.LogWarning("Plex device added but server not found: {Name}", serverName);
            return;
        }
        logger.LogInformation("Plex device added: {Name} ({IPAddress})", serverName, server.Info.IPAddress);
        if (!this._servers.TryAdd(serverName, server))
        {
            return;
        }
        server.SelectedPlayerChanged
            .TakeUntil(server.Disposed)
            .Subscribe(player => this.OnSelectedPlayerChanged(server, player));
        server.PlayStateChanged
            .DistinctUntilChanged()
            .TakeUntil(server.Disposed)
            .Subscribe(state => this.OnPlayStateChanged(server, state));
        server.ActiveMediaChanged
            .DistinctUntilChanged()
            .TakeUntil(server.Disposed)
            .Subscribe(media => this.OnActiveMediaChanged(server, media));
    }

    private async Task HandleDeviceRemovedAsync(string serverName, CancellationToken cancellationToken)
    {
        if (this._servers.TryRemove(serverName, out IPlexServer? server))
        {
            logger.LogInformation("Plex device removed: {serverName}", serverName);
            server.Dispose();
        }
    }

    private async Task<ActionResult> HandleDeviceRouteAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        int index = path.IndexOf('/');
        if (index == -1)
        {
            return new NotFoundResult();
        }
        string prefix = path[..index];
        string suffix = path[(index + 1)..];
        return prefix switch
        {
            VirtualDirectories.Thumbnail => await HandleThumbnailRouteAsync(suffix).ConfigureAwait(false),
            VirtualDirectories.Embedded => await HandleEmbeddedImageRouteAsync(suffix).ConfigureAwait(false),
            _ => new NotFoundResult(),
        };

        async Task<ActionResult> HandleThumbnailRouteAsync(string suffix)
        {
            string base64 = suffix.Replace('-', '+').Replace('_', '/');
            if (((4 - (base64.Length % 4)) % 4) is int padding && padding > 0)
            {
                base64 += new string('=', padding);
            }
            try
            {
                string url = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                using HttpRequestMessage plexRequest = new(HttpMethod.Get, url);
                plexRequest.Headers.Add("X-Plex-Token", tokenStore.AuthToken);
                using HttpResponseMessage plexResponse = await _httpClient.SendAsync(plexRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!plexResponse.IsSuccessStatusCode)
                {
                    return new NotFoundResult();
                }
                Stream stream = await plexResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return new FileStreamResult(stream, plexResponse.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
            }
            catch (FormatException)
            {
                return new BadRequestResult(); // Invalid base64
            }
            catch (HttpRequestException)
            {
                return new StatusCodeResult(503); // Service unavailable.
            }
        }

        async Task<ActionResult> HandleEmbeddedImageRouteAsync(string suffix)
        {
            string name = $"{typeof(IPlexServer).Namespace}.Images.{suffix.Replace('/', '.')}";
            if (Assembly.GetExecutingAssembly().GetManifestResourceStream(name) is not { } stream)
            {
                return new NotFoundResult();
            }
            string fileName = path.LastIndexOf('/') is int index and not -1 ? path[(index + 1)..] : path;
            byte[] bytes = new byte[(int)stream.Length];
            await stream.ReadAsync(bytes, cancellationToken).ConfigureAwait(false);
            return new FileContentResult(bytes, PlexDeviceProviderBase.GetContentType(fileName)) { FileDownloadName = fileName };
        }
    }

    private async Task HandleSelectPlayerAsync(string serverName, string machineIdentifier, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is { } server)
        {
            await server.SelectPlayerAsync(machineIdentifier, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing...");
        await discovery.InitializeAsync().ConfigureAwait(false);
        if (Interlocked.Exchange(ref this._initialServerNames, null) is not { Length: > 0 } names)
        {
            return;
        }
        try
        {
            await Parallel.ForEachAsync(
                names,
                cancellationToken,
                async (name, cancellationToken) => await this.HandleDeviceAddedAsync(name, cancellationToken).ConfigureAwait(false)
            );
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private bool IsConnected(string serverName) => this.GetServer(serverName) is { IsConnected: true };

    private async void OnActiveMediaChanged(IPlexServer server, MediaItem? media)
    {
        if (this._notifier is not { } notifier)
        {
            return;
        }
    }

    private async void OnPlayStateChanged(IPlexServer server, PlayState state)
    {
        if (this._notifier is not { } notifier)
        {
            return;
        }
        logger.LogInformation("Logging play state - {Name}:{State}", server.Name, state);
        await notifier.SendNotificationAsync(Components.Playing.SensorName, state is PlayState.Playing or PlayState.Buffering, server.Name);
    }

    private async void OnSelectedPlayerChanged(IPlexServer server, PlexPlayerInfo? player)
    {
        if (this._notifier is { } notifier)
        {
            await notifier.SendNotificationAsync(Components.Player.SensorName, player?.Name ?? string.Empty, server.Name).ConfigureAwait(false);
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
        // Clear any invalid token.
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

    private void SetInitialServerNames(string[] serverNames)
    {
        logger.LogInformation("Set server names: [{Names}]", string.Join(',', serverNames));
        this._initialServerNames = serverNames;
    }

    private void SetUriPrefix(string prefix)
    {
        this._uriPrefix = prefix;
        this._embeddedImages.UriPrefix = prefix;
    }

    protected static class Components
    {
        public static readonly PlexComponent CoverArt = "COVER_ART";
        public static readonly PlexComponent Description = "DESCRIPTION";
        public static readonly PlexComponent Player = "PLAYER";
        public static readonly PlexComponent Playing = "PLAYING";
        public static readonly PlexComponent Title = "TITLE";

        public readonly struct PlexComponent(string name)
        {
            public string Name => name;

            public string SensorName => $"{name}_SENSOR";

            public static implicit operator PlexComponent(string name) => new(name);

            public static implicit operator string(PlexComponent component) => component.Name;
        }
    }

    protected static class VirtualDirectories
    {
        public const string Embedded = "embedded";

        public const string Thumbnail = "thumbnail";
    }

    protected sealed class EmbeddedImages
    {
        public string Logo => this.GetEmbeddedResourceUrl("plex_logo.png");

        public string Player => this.GetEmbeddedResourceUrl("player.jpg");

        public string UriPrefix { get; set; } = string.Empty;

        private string GetEmbeddedResourceUrl(string suffix) => $"{this.UriPrefix}{VirtualDirectories.Embedded}/{suffix}";
    }
}
