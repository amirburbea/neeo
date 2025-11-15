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
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public abstract class PlexDeviceProviderBase(
    IHttpClientFactory httpClientFactory,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
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
    private string[]? _initialServerIds;
    private IDeviceNotifier? _notifier;
    private string _uriPrefix = string.Empty;

    public IDeviceBuilder DeviceBuilder => field ??= this.CreateDevice();

    public void Dispose()
    {
        this._httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    protected Task BrowseDirectoryAsync(string machineIdentifier, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetServer(machineIdentifier) is not { } server || builder.Parameters.BrowseIdentifier is not { } identifier)
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
        .AddTextLabel(Components.Player, "Player:", (machineIdentifier) => this.GetServer(machineIdentifier)?.SelectedPlayerName ?? string.Empty)
        .EnableDeviceRoute(uriPrefix => this._uriPrefix = uriPrefix, this.HandleHttpRequestAsync)
        .EnableDiscovery("Discovering Plex Servers", "Select Plex Server", (optionalDeviceId, _) => Task.FromResult(this.GetDiscoveredServers(optionalDeviceId)))
        .EnableNotifications(notifier => this._notifier = notifier)
        .EnableRegistration("Plex Registration", "Enter your credentials to connect to Plex", this.QueryIsRegisteredAsync, this.RegisterAsync)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, async (serverIds, _) => this._initialServerIds = serverIds)
        .RegisterInitializer(this.InitializeAsync)
        .AddButtonGroup(ButtonGroups.Power)
        .SetManufacturer("Plex");

    protected string GetCoverArt(string machineIdentifier) => this.GetCoverArt(this.GetServer(machineIdentifier)?.ActiveMedia);

    protected string GetDescription(string machineIdentifier) => PlexDeviceProviderBase.GetDescription(this.GetServer(machineIdentifier)?.ActiveMedia);

    protected string GetEmbeddedResourceUrl(EmbeddedImage image)
    {
        string fileName = TextAttribute.GetText(image);
        return $"{this._uriPrefix}{VirtualDirectories.Embedded}/{fileName}";
    }

    protected IPlexServer? GetServer(string machineIdentifier) => this._servers.GetValueOrDefault(machineIdentifier);

    protected string GetThumbnailUrl(Uri plexUri, ImageSize size = ImageSize.Large)
    {
        int pixels = size is ImageSize.Large ? 480 : 100;
        string suffix = PlexDeviceProviderBase.Encoded($"{plexUri}&width={pixels}&height={pixels}");
        return $"{this._uriPrefix}{VirtualDirectories.Thumbnail}/{suffix}";
    }

    protected string GetTitle(string machineIdentifier)
    {
        if (this.GetServer(machineIdentifier) is not { IsConnected: true } server)
        {
            return "Server not connected";
        }
        if (server.SelectedPlayer is null)
        {
            return "Player not selected";
        }
        return server.ActiveMedia?.Title ?? "Nothing is playing";
    }

    protected async Task HandleDirectoryActionAsync(string machineIdentifier, string actionIdentifier, CancellationToken cancellationToken)
    {
        int index = actionIdentifier.IndexOf('.');
        if (index == -1)
        {
            return;
        }
        string selection = actionIdentifier[(index + 1)..];
        await (actionIdentifier[..index] switch
        {
            "player" => this.SelectPlayerAsync(machineIdentifier, selection, cancellationToken),
            _ => Task.CompletedTask,
        });
    }

    protected async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await serverManager.InitializeAsync().ConfigureAwait(false);
        if (Interlocked.Exchange(ref this._initialServerIds, null) is { Length: > 0 } machineIds)
        {
            await Parallel.ForEachAsync(
                machineIds,
                cancellationToken,
                (machineIdentifier, token) => new(this.OnDeviceAddedAsync(machineIdentifier, token))
            ).ConfigureAwait(false);
        }
    }

    protected bool IsPlaying(string machineIdentifier) => this.GetServer(machineIdentifier) is { PlayState: PlayState.Playing or PlayState.Buffering };

    private static string Decoded(string text)
    {
        string base64 = $"{text[..^1].Replace('-', '+').Replace('_', '/')}{new string('=', int.Parse(text[^1..]))}";
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string Encoded(string text)
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

    private async Task BrowsePlayersAsync(IPlexServer server, bool refresh, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        List<PlexPlayerData> players = [..
            from tuple in await server.ListPlayersAsync(refresh, cancellationToken).ConfigureAwait(false)
            where tuple.IsOnline
            select tuple.Player
        ];
        if (players.Count == 0)
        {
            builder.AddEntry(new(Title: "Clients not found!", Label: "Click to attempt reloading the list", BrowseIdentifier: ".player.refresh"));
            return;
        }
        builder.AddHeader("Available Players");
        foreach (PlexPlayerData player in players)
        {
            builder.AddEntry(new(
                player.Name,
                ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Player),
                ActionIdentifier: $"player.{player.MachineIdentifier}",
                UIAction: DirectoryUIAction.Close
            ));
        }
    }

    private async Task BrowseRootMenuAsync(string machineIdentifier, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetServer(machineIdentifier) is not { } server)
        {
            return;
        }
        builder
            .AddTileRow([new(this.GetEmbeddedResourceUrl(EmbeddedImage.Logo))])
            .AddEntry(new(
                server.SelectedPlayerName is { } name ? $"Switch player ({name})" : "Select player",
                BrowseIdentifier: ".player",
                ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Player)
            ));
        if (server.SelectedPlayer == null)
        {
            return;
        }
        foreach (LibrarySection section in await server.GetLibrarySectionsAsync(cancellationToken).ConfigureAwait(false))
        {
            builder.AddEntry(new(
                section.Title,
                ThumbnailUri: this.GetEmbeddedResourceUrl(section.Type.GetMediaType() switch
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
        ThumbnailUri: media.ThumbnailUri is { } uri ? this.GetThumbnailUrl(uri, ImageSize.Small) : null
    );

    private string GetCoverArt(MediaItem? media) => media is { ThumbnailUri: { } uri }
        ? this.GetThumbnailUrl(uri)
        : this.GetEmbeddedResourceUrl(EmbeddedImage.Logo);

    private DiscoveredDevice[] GetDiscoveredServers(string? optionalMachineIdentifier)
    {
        return optionalMachineIdentifier switch
        {
            null => [.. serverManager.ServerData.Select(ServerDiscoveryResult)],
            { Length: > 0 } id when serverManager.TryGetServerData(id) is { } data => [ServerDiscoveryResult(data)],
            _ => []
        };

        DiscoveredDevice ServerDiscoveryResult(PlexServerData data) => new(data.MachineIdentifier, $"{data.Name} ({deviceName})");
    }

    private async Task HandleButtonAsync(string machineIdentifier, string buttonName, CancellationToken cancellationToken)
    {
        if (this.GetServer(machineIdentifier) is { } server &&
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
                using HttpRequestMessage request = new(HttpMethod.Get, new Uri(PlexDeviceProviderBase.Decoded(suffix))) { Headers = { { "X-Plex-Token", tokenStore.AuthToken } } };
                HttpResponseMessage response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new StatusCodeResult((int)response.StatusCode);
                }
                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return new FileStreamResult(stream, response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
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

    private bool IsConnected(string machineIdentifier) => this.GetServer(machineIdentifier) is { IsConnected: true };

    private async Task OnDeviceAddedAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (serverManager.GetServer(machineIdentifier) is not { } server)
        {
            logger.LogWarning("Plex device added but server not found: {Id}", machineIdentifier);
            return;
        }
        logger.LogInformation("Plex device added: {Name} ({IPAddress})", server.Name, server.Data.IPAddress);
        if (!this._servers.TryAdd(machineIdentifier, server) || this._notifier is not { } notifier)
        {
            return;
        }
        // Notify on changes to connection.
        server.IsConnectedChanged
            .TakeUntil(server.Disposed)
            .DistinctUntilChanged()
            .Select(isConnected => Observable.FromAsync((token) => notifier.SendPowerNotificationAsync(isConnected, machineIdentifier, token)))
            .Switch()
            .Subscribe();
        // Notify on changes to player.
        server.SelectedPlayerChanged
            .Select(tuple => tuple.HasValue ? tuple.Value.Player.Name : string.Empty)
            .TakeUntil(server.Disposed)
            .DistinctUntilChanged()
            .Select(playerName => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Player.SensorName, playerName, machineIdentifier, token)))
            .Switch()
            .Subscribe();
        // Notify on changes to play state.
        server.PlayStateChanged
            .TakeUntil(server.Disposed)
            .Select(state => state is PlayState.Playing or PlayState.Buffering)
            .DistinctUntilChanged()
            .Select(isPlaying => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Playing.SensorName, isPlaying, machineIdentifier, token)))
            .Switch()
            .Subscribe();
        // Notify on changes to active media.
        server.ActiveMediaChanged
            .TakeUntil(server.Disposed)
            .DistinctUntilChanged()
            .Select(media => Observable.FromAsync((token) => Task.WhenAll(
                notifier.SendNotificationAsync(Components.Description.SensorName, PlexDeviceProviderBase.GetDescription(media), machineIdentifier, token),
                notifier.SendNotificationAsync(Components.CoverArt.SensorName, this.GetCoverArt(media), machineIdentifier, token)
            )))
            .Switch()
            .Subscribe();
        // Title is affected by server connection, selected player and active media.
        server.IsConnectedChanged
            .TakeUntil(server.Disposed)
            .CombineLatest(server.SelectedPlayerChanged, server.ActiveMediaChanged)
            .DistinctUntilChanged()
            .Select(_ => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Title.SensorName, this.GetTitle(machineIdentifier), machineIdentifier, token)))
            .Switch()
            .Subscribe();
        await server.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task OnDeviceRemovedAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (this._servers.TryRemove(machineIdentifier, out IPlexServer? server))
        {
            logger.LogInformation("Plex device removed: {Name}", machineIdentifier);
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

    private async Task<RegistrationResult> RegisterAsync(string userName, string password, CancellationToken cancellationToken)
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

    private async Task SelectPlayerAsync(string machineIdentifier, string playerIdentifier, CancellationToken cancellationToken)
    {
        if (this.GetServer(machineIdentifier) is { } server)
        {
            await server.SelectPlayerAsync(playerIdentifier, cancellationToken).ConfigureAwait(false);
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
