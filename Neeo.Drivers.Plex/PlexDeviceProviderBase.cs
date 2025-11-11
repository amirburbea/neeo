using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Neeo.Sdk;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public abstract class PlexDeviceProviderBase(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    Task<ISdkEnvironment> sdkStartupTask,
    ILogger logger,
    DeviceType deviceType,
    string deviceName
) : IDeviceProvider, IDisposable
{
    protected static readonly IReadOnlyDictionary<Buttons, Func<IPlexServer, CancellationToken, Task>> ButtonHandlers = new Dictionary<Buttons, Func<IPlexServer, CancellationToken, Task>>()
    {
        { Buttons.Pause, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.Pause, token) },
        { Buttons.Play, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.Play, token) },
        { Buttons.Stop, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.Stop, token) },
        { Buttons.Forward, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.StepForward, token) },
        { Buttons.Back, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.StepBack, token) },
        { Buttons.SkipForward,  (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.SkipNext, token) },
        { Buttons.SkipBackward, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.SkipPrevious, token) },
        { Buttons.CursorUp, (server, token) => server.SendNavigationCommandAsync(NavigationCommand.MoveUp, token) },
        { Buttons.CursorDown, (server, token) => server.SendNavigationCommandAsync(NavigationCommand.MoveDown, token) },
        { Buttons.CursorLeft, (server, token) => server.SendNavigationCommandAsync(NavigationCommand.MoveLeft, token) },
        { Buttons.CursorRight, (server, token) => server.SendNavigationCommandAsync(NavigationCommand.MoveRight, token) },
        { Buttons.CursorEnter, (server, token) => server.SendNavigationCommandAsync(NavigationCommand.Select, token) },
    };

    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");
    private static readonly Uri _userUri = new($"https://plex.tv/api/v2/user");

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly ConcurrentDictionary<string, IPlexServer> _servers = [];
    private IDeviceNotifier? _notifier;
    private string _uriPrefix = string.Empty;

    public IDeviceBuilder DeviceBuilder => field ??= this.CreateDevice();

    public void Dispose()
    {
        this._httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    protected Task BrowseDirectoryAsync(string serverName, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is not { } server || builder.Parameters.BrowseIdentifier is not { } identifier)
        {
            return Task.CompletedTask;
        }
        return identifier switch
        {
            "" => this.BrowseRootMenuAsync(server.Name, builder, cancellationToken),
            ".player" => this.BrowsePlayersAsync(server, refresh: false, builder, cancellationToken),
            ".player.refresh" => this.BrowsePlayersAsync(server, refresh: true, builder, cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(deviceName, deviceType)
        .AddAdditionalSearchTokens("PMS")
        .AddButtonHandler(this.HandleButtonAsync)
        .AddPowerStateSensor(this.IsConnected)
        .AddTextLabel(Components.Player, "Player:", (serverName) => this.GetServer(serverName)?.SelectedPlayer?.Name ?? string.Empty)
        .EnableDeviceRoute(uriPrefix => this._uriPrefix = uriPrefix, this.HandleHttpRequestAsync)
        .EnableDiscovery("Discovering Plex Servers", "Select Plex Server", (optionalDeviceId, _) => Task.FromResult(this.GetDiscoveredServers(optionalDeviceId)))
        .EnableNotifications(notifier => this._notifier = notifier)
        .EnableRegistration("Plex Registration", "Enter your credentials to connect to Plex", this.QueryIsRegisteredAsync, this.RegisterDeviceAsync)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, async (ids, _) => this.InitializeDeviceList(ids))
        .RegisterInitializer((_) => discovery.InitializeAsync())
        .AddButtonGroup(ButtonGroups.Power)
        .SetManufacturer("Plex")

        .SetDriverVersion(10);

    protected string GetCoverArt(string serverName) => this.GetCoverArt(this.GetServer(serverName)?.ActiveMedia);

    protected string GetDescription(string serverName) => PlexDeviceProviderBase.GetDescription(this.GetServer(serverName)?.ActiveMedia);

    protected string GetEmbeddedResourceUri(EmbeddedImage image)
    {
        string fileName = TextAttribute.GetText(image);
        return $"{this._uriPrefix}{VirtualDirectories.Embedded}/{fileName}";
    }

    protected IPlexServer? GetServer(string serverName) => this._servers.GetValueOrDefault(serverName);

    protected string GetThumbnailUri(Uri plexUri, ImageSize size = ImageSize.Large)
    {
        int pixels = size is ImageSize.Large ? 480 : 100;
        string suffix = PlexDeviceProviderBase.EncodeUri($"{plexUri}&width={pixels}&height={pixels}");
        return $"{this._uriPrefix}{VirtualDirectories.Thumbnail}/{suffix}";
    }

    protected string GetTitle(string serverName)
    {
        if (this.GetServer(serverName) is not { IsConnected: true } server)
        {
            return "Server not connected";
        }
        if (server.SelectedPlayer is null)
        {
            return "Player not selected";
        }
        return server.ActiveMedia?.Title ?? "Nothing is playing";
    }

    protected Task HandleDirectoryActionAsync(string serverName, string actionIdentifier, CancellationToken cancellationToken)
    {
        int index = actionIdentifier.IndexOf('.');
        if (index == -1)
        {
            return Task.CompletedTask;
        }
        string selection = actionIdentifier[(index + 1)..];
        return actionIdentifier[..index] switch
        {
            "player" => this.SelectPlayerAsync(serverName, selection, cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    protected bool IsPlaying(string serverName) => this.GetServer(serverName) is { PlayState: PlayState.Playing or PlayState.Buffering };

    private static string DecodeUri(string text)
    {
        string base64 = $"{text[..^1].Replace('-', '+').Replace('_', '/')}{new string('=', int.Parse(text[^1..]))}";
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string EncodeUri(string text)
    {
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).Replace('+', '-').Replace('/', '_');
        string trimmed = base64.TrimEnd('=');
        return $"{trimmed}{base64.Length - trimmed.Length}";
    }

    private static string GetContentType(string fileName) => PlexDeviceProviderBase._contentTypeProvider.TryGetContentType(fileName, out string? contentType)
        ? contentType
        : "application/octet-stream";

    private static string GetDescription(MediaItem? media) => media switch
    {
        { Summary: { Length: > 0 } summary } => summary,
        { Title: { } title } => title,
        _ => string.Empty,
    };

    private static string? GetSelectedPlayerName(IPlexServer server) => server?.SelectedPlayer?.Name;

    private async Task BrowsePlayersAsync(IPlexServer server, bool refresh, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (await server.ListPlayersAsync(refresh, cancellationToken).ConfigureAwait(false) is not { Length: > 0 } players)
        {
            builder.AddEntry(new(Title: "Clients not found!", Label: "Click to attempt reloading the list", BrowseIdentifier: ".player.refresh"));
            return;
        }
        builder.AddHeader("Available Players");
        foreach (PlexPlayerInfo player in players)
        {
            builder.AddEntry(new(
                player.Name,
                ThumbnailUri: this.GetEmbeddedResourceUri(EmbeddedImage.Player),
                ActionIdentifier: $"player.{player.MachineIdentifier}",
                UIAction: DirectoryUIAction.Close
            ));
        }
        builder.SetTotalMatchingItems(players.Length);
    }

    private async Task BrowseRootMenuAsync(string serverName, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is not { } server)
        {
            return;
        }
        builder
            .AddTileRow([new(this.GetEmbeddedResourceUri(EmbeddedImage.Logo))])
            .AddEntry(new(
                server.SelectedPlayer is { } name ? $"Select player ({name})" : "Select player",
                BrowseIdentifier: ".player",
                ThumbnailUri: this.GetEmbeddedResourceUri(EmbeddedImage.Player)
            ));
        if (server.SelectedPlayer == null)
        {
            return;
        }
        foreach (LibrarySection section in await server.GetLibrarySectionsAsync(cancellationToken).ConfigureAwait(false))
        {
            builder.AddEntry(new(
                section.Title,
                ThumbnailUri: this.GetEmbeddedResourceUri(section.Type.GetMediaType() switch
                {
                    MediaType.Music => EmbeddedImage.Music,
                    MediaType.Video => EmbeddedImage.Movie,
                    MediaType.Episode => EmbeddedImage.TVShow,
                    _ => EmbeddedImage.Menu,
                }),
                BrowseIdentifier: $".library.{section.Key}"
            ));
        }
    }

    private DirectoryEntry CreateEntry(MediaItem media) => new(
        media.Title,
        media.Summary,
        ActionIdentifier: $".media.{media.RatingKey}",
        UIAction: DirectoryUIAction.Close,
        ThumbnailUri: media.ThumbnailUri is { } uri ? this.GetThumbnailUri(uri, ImageSize.Small) : null
    );

    private string GetCoverArt(MediaItem? media) => media is { ThumbnailUri: { } uri }
        ? this.GetThumbnailUri(uri)
        : this.GetEmbeddedResourceUri(EmbeddedImage.Logo);

    private DiscoveredDevice[] GetDiscoveredServers(string? optionalDeviceId)
    {
        return optionalDeviceId switch
        {
            null => [.. discovery.Servers.Values.Select(ServerDiscoveryResult)],
            { Length: > 0 } id when discovery.Servers.TryGetValue(id, out PlexServerData info) => [ServerDiscoveryResult(info)],
            _ => []
        };

        DiscoveredDevice ServerDiscoveryResult(PlexServerData info) => new(info.Name, $"{info.Name} ({deviceName})");
    }

    private async Task HandleButtonAsync(string serverName, string buttonName, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is { } server &&
            Button.TryResolve(buttonName) is { } button &&
            PlexDeviceProviderBase.ButtonHandlers.GetValueOrDefault(button) is { } handler)
        {
            await handler(server, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ActionResult> HandleHttpRequestAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        int index = path.IndexOf('/');
        if (index == -1)
        {
            return new NotFoundResult();
        }
        string suffix = path[(index + 1)..];
        return path[..index] switch
        {
            VirtualDirectories.Embedded => await HandleEmbeddedImageRouteAsync(suffix).ConfigureAwait(false),
            VirtualDirectories.Thumbnail => await HandleThumbnailRouteAsync(suffix).ConfigureAwait(false),
            _ => new NotFoundResult(),
        };

        static async Task<ActionResult> HandleEmbeddedImageRouteAsync(string fileName)
        {
            if (Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(PlexServer).Namespace}.Images.{fileName}") is not { } stream)
            {
                return new NotFoundResult();
            }
            return new FileStreamResult(stream, PlexDeviceProviderBase.GetContentType(fileName)) { FileDownloadName = fileName };
        }

        async Task<ActionResult> HandleThumbnailRouteAsync(string suffix)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, PlexDeviceProviderBase.DecodeUri(suffix)) { Headers = { { "X-Plex-Token", tokenStore.AuthToken } } };
                HttpResponseMessage response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new StatusCodeResult((int)response.StatusCode);
                }
                return new FileStreamResult(
                    await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                    response.Content.Headers.ContentType?.MediaType ?? "image/jpeg"
                );
            }
            catch (FormatException)
            {
                return new BadRequestResult();
            }
            catch (HttpRequestException)
            {
                return new StatusCodeResult(503);
            }
        }
    }

    private void InitializeDeviceList(string[] serverNames)
    {
        logger.LogInformation("Set server names: [{Names}]", string.Join(',', serverNames));
        if (serverNames.Length != 0)
        {
            _ = Task.Run(InitializeAfterStartup, cancellationToken: default);
        }

        async Task InitializeAfterStartup()
        {
            await sdkStartupTask.ConfigureAwait(false);
            await discovery.InitializeAsync().ConfigureAwait(false);
            await Parallel.ForEachAsync(
                serverNames,
                default(CancellationToken),
                (name, cancellationToken) => new(this.OnDeviceAddedAsync(name, cancellationToken))
            ).ConfigureAwait(false);
        }
    }

    private bool IsConnected(string serverName) => this.GetServer(serverName) is { IsConnected: true };

    private async Task OnDeviceAddedAsync(string serverName, CancellationToken cancellationToken)
    {
        if (serverManager.GetServer(serverName) is not { } server)
        {
            logger.LogWarning("Plex device added but server not found: {Name}", serverName);
            return;
        }
        logger.LogInformation("Plex device added: {Name} ({IPAddress})", serverName, server.Info.IPAddress);
        if (!this._servers.TryAdd(serverName, server) || this._notifier is not { } notifier)
        {
            return;
        }
        // Notify on changes to connection.
        server.IsConnectedChanged
            .TakeUntil(server.Disposed)
            .DistinctUntilChanged()
            .Select(isConnected => Observable.FromAsync((token) => notifier.SendPowerNotificationAsync(isConnected, serverName, token)))
            .Switch()
            .Subscribe();
        // Notify on changes to player.
        server.SelectedPlayerChanged
            .TakeUntil(server.Disposed)
            .DistinctUntilChanged()
            .Select(player => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Player.SensorName, player?.Name ?? string.Empty, serverName, token)))
            .Switch()
            .Subscribe();
        // Notify on changes to play state.
        server.PlayStateChanged
            .TakeUntil(server.Disposed)
            .Select(state => state is PlayState.Playing or PlayState.Buffering)
            .DistinctUntilChanged()
            .Select(isPlaying => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Playing.SensorName, isPlaying, serverName, token)))
            .Switch()
            .Subscribe();
        // Notify on changes to active media.
        server.ActiveMediaChanged
            .TakeUntil(server.Disposed)
            .DistinctUntilChanged()
            .Select(media => Observable.FromAsync((token) => Task.WhenAll(
                notifier.SendNotificationAsync(Components.Description.SensorName, PlexDeviceProviderBase.GetDescription(media), serverName, token),
                notifier.SendNotificationAsync(Components.CoverArt.SensorName, this.GetCoverArt(media), serverName, token)
            )))
            .Switch()
            .Subscribe();
        // Title is affected by server connection, selected player and active media.
        server.IsConnectedChanged
            .TakeUntil(server.Disposed)
            .CombineLatest(server.SelectedPlayerChanged, server.ActiveMediaChanged)
            .DistinctUntilChanged()
            .Select(_ => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Title.SensorName, this.GetTitle(serverName), serverName, token)))
            .Switch()
            .Subscribe();
        await server.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task OnDeviceRemovedAsync(string serverName, CancellationToken cancellationToken)
    {
        if (this._servers.TryRemove(serverName, out IPlexServer? server))
        {
            logger.LogInformation("Plex device removed: {Name}", serverName);
            server.Dispose();
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
                return HttpStatusCode.OK == await this._httpClient.HeadAsync(
                    PlexDeviceProviderBase._userUri,
                    request => request.Headers.Add("X-Plex-Token", tokenStore.AuthToken),
                    cancellationToken
                ).ConfigureAwait(false);
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

    private async Task SelectPlayerAsync(string serverName, string machineIdentifier, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is { } server)
        {
            await server.SelectPlayerAsync(machineIdentifier, cancellationToken).ConfigureAwait(false);
        }
    }

    protected enum EmbeddedImage
    {
        [Text("plex_logo.png")]
        Logo = 1,

        [Text("player.jpg")]
        Player = 2,

        [Text("music.jpg")]
        Music = 3,

        [Text("tv_show.jpg")]
        TVShow = 4,

        [Text("movie.jpg")]
        Movie = 5,

        [Text("menu.jpg")]
        Menu = 6
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
}
