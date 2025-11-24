using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

public abstract partial class PlexDeviceProviderBase(
    IHttpClientFactory httpClientFactory,
    IPlexServerManager serverManager,
    IPlexTokenStore tokenStore,
    Task<ISdkEnvironment> startupTask,
    ILogger logger,
    DeviceType deviceType,
    string deviceName
) : IDeviceProvider, IDisposable
{
    protected static readonly Dictionary<Buttons, Func<IPlexServer, CancellationToken, Task>> ButtonHandlers = new()
    {
        { Buttons.PlayToggle, PlexDeviceProviderBase.TogglePlayAsync },
        { Buttons.Pause, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.Pause, token) },
        { Buttons.Play, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.Play, token) },
        { Buttons.Forward, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.StepForward, token) },
        { Buttons.Back, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.StepBack, token) },
        { Buttons.NextTrack,  (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.SkipNext, token) },
        { Buttons.SkipForward,  (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.SkipNext, token) },
        { Buttons.PreviousTrack, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.SkipPrevious, token) },
        { Buttons.SkipBackward, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.SkipPrevious, token) },
        { Buttons.Stop, (server, token) => server.SendPlaybackCommandAsync(PlaybackCommand.Stop, token) },
        { Buttons.CursorUp, (server, token) => Task.CompletedTask },
        { Buttons.CursorDown, (server, token) => Task.CompletedTask },
        { Buttons.CursorLeft, (server, token) => server.SeekBackAsync(token) },
        { Buttons.CursorRight, (server, token) => server.SeekForwardAsync(token) },
        { Buttons.CursorEnter, PlexDeviceProviderBase.TogglePlayAsync },
    };

    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");
    private static readonly Uri _userUri = new($"https://plex.tv/api/v2/user");

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(Plex));
    private readonly ConcurrentDictionary<string, IPlexServer> _servers = [];
    private IDeviceNotifier? _notifier;
    private string _uriPrefix = string.Empty;

    public IDeviceBuilder DeviceBuilder => field ??= this.CreateDevice();

    public void Dispose()
    {
        this._httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    protected Task BrowseDirectoryAsync(string machineIdentifier, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetServer(machineIdentifier) is not { } server || builder.BrowseIdentifier is not { } browseIdentifier)
        {
            return Task.CompletedTask;
        }
        int index = browseIdentifier.IndexOf('.');
        string prefix = index is not -1 ? browseIdentifier[..index] : browseIdentifier;
        string suffix = index == -1 ? string.Empty : browseIdentifier[(index + 1)..];
        return prefix switch
        {
            "" => this.BrowseRootAsync(server, builder, cancellationToken),
            "player" => this.BrowsePlayersAsync(server, refresh: suffix == "refresh", builder, cancellationToken),
            "library" => this.BrowseLibraryAsync(server, suffix, builder, cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(deviceName, deviceType)
        .AddAdditionalSearchTokens("PMS")
        .AddButtonHandler(this.HandleButtonAsync)
        .AddPowerStateSensor(this.IsConnected)
        .AddTextLabel(Components.Player, "Player", (machineIdentifier) => this.GetServer(machineIdentifier)?.SelectedPlayerName ?? string.Empty)
        .EnableDeviceRoute(uriPrefix => this._uriPrefix = uriPrefix, this.HandleHttpRequestAsync)
        .EnableDiscovery("Discovering Plex Servers", "Select Plex Server", (optionalDeviceId, _) => Task.FromResult(this.GetDiscoveredServers(optionalDeviceId)))
        .EnableNotifications(notifier => this._notifier = notifier)
        .EnableRegistration("Plex Registration", "Enter your credentials to connect to Plex", this.QueryIsRegisteredAsync, this.RegisterAsync)
        .RegisterDeviceSubscriptionCallbacks(this.OnServerAddedAsync, this.OnServerRemovedAsync, this.NotifyInitialServersAsync)
        .RegisterInitializer(this.InitializeAsync)
        .AddButtonGroup(ButtonGroups.Power)
        .SetManufacturer("Plex")
        .SetDriverVersion(2);

    protected string GetCoverArt(string machineIdentifier) => this.GetCoverArt(this.GetServer(machineIdentifier)?.ActiveMedia);

    protected string GetDescription(string machineIdentifier) => this.GetActiveMediaText(
        machineIdentifier,
        media => media.Summary is { Length: > 0 } summary ? summary : media.Title
    );

    protected IPlexServer? GetServer(string machineIdentifier) => this._servers.GetValueOrDefault(machineIdentifier);

    protected string GetThumbnailUrl(Uri plexUri, ImageSize size = ImageSize.Large)
    {
        int pixels = size is ImageSize.Large ? 480 : 100;
        string suffix = PlexDeviceProviderBase.EncodeBase64($"{plexUri}&width={pixels}&height={pixels}");
        return $"{this._uriPrefix}{VirtualDirectories.Thumbnail}/{suffix}";
    }

    protected string GetTitle(string machineIdentifier) => this.GetActiveMediaText(machineIdentifier, static media => media.Title);

    protected Task HandleDirectoryActionAsync(string machineIdentifier, string actionIdentifier, CancellationToken cancellationToken)
    {
        if (this.GetServer(machineIdentifier) is not { } server || actionIdentifier.IndexOf('.') is not (int index and not -1))
        {
            return Task.CompletedTask;
        }
        string suffix = actionIdentifier[(index + 1)..];
        return (actionIdentifier[..index] switch
        {
            "player" => server.SelectPlayerAsync(suffix, cancellationToken),
            "media" => server.PlayMediaAsync(int.Parse(suffix), cancellationToken),
            _ => Task.CompletedTask,
        });
    }

    protected async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await serverManager.InitializeAsync().ConfigureAwait(false);
    }

    protected bool IsPlaying(string machineIdentifier) => this.GetServer(machineIdentifier) is { PlayState: PlayState.Playing or PlayState.Buffering };

    private static string DecodeBase64(string text)
    {
        string base64 = $"{text[..^1].Replace('-', '+').Replace('_', '/')}{new string('=', int.Parse(text[^1..]))}";
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string EncodeBase64(string text)
    {
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).Replace('+', '-').Replace('/', '_');
        string trimmed = base64.TrimEnd('=');
        return $"{trimmed}{base64.Length - trimmed.Length}";
    }

    private static string GetContentType(string fileName) => PlexDeviceProviderBase._contentTypeProvider.TryGetContentType(fileName, out string? contentType)
        ? contentType
        : "application/octet-stream";

    [GeneratedRegex(@"^(?<key>\d+)\:(?<type>\d+)(\.(?<parameters>.+))?$", RegexOptions.ExplicitCapture)]
    private static partial Regex IdentifierRegex();

    private static async Task TogglePlayAsync(IPlexServer server, CancellationToken cancellationToken)
    {
        if (server is { SelectedPlayer.IsOnline: true, ActiveMedia: not null, PlayState: { } state })
        {
            await server.SendPlaybackCommandAsync(state is PlayState.Paused ? PlaybackCommand.Play : PlaybackCommand.Pause, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task BrowseLibraryAsync(IPlexServer server, string identifier, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (PlexDeviceProviderBase.IdentifierRegex().Match(identifier) is not { Success: true, Groups: { } groups })
        {
            return Task.CompletedTask;
        }
        int sectionKey = int.Parse(groups["key"].Value);
        LibrarySectionType sectionType = (LibrarySectionType)int.Parse(groups["type"].Value);
        if (groups["parameters"].Value.Length is 0)
        {
            return BrowseSectionRootAsync();
        }
        PaginationParameters parameters = new(offset: builder.Parameters.Offset, pageSize: builder.Parameters.Limit);
        return groups["parameters"].Value.Split('.') switch
        {
            ["all"] => BrowseAllAsync(),
            ["firstCharacter"] => BrowseFirstCharactersAsync(),
            ["firstCharacter", { Length: 1 } character] => BrowseFirstCharacterAsync(character[0]),
            ["recentlyAdded"] => BrowseRecentlyAddedAsync(),
            _ => Task.CompletedTask,
        };

        Task BrowseAllAsync() => sectionType switch
        {
            LibrarySectionType.Movie => BrowseDirectoryAsync(server.Library.ListMoviesAsync(sectionKey, parameters, cancellationToken)),
            LibrarySectionType.Show => BrowseDirectoryAsync(server.Library.ListTVShowsAsync(sectionKey, parameters, cancellationToken)),
            LibrarySectionType.Artist => BrowseDirectoryAsync(server.Library.ListMusicAsync(sectionKey, parameters, cancellationToken)),
            _ => Task.CompletedTask,
        };

        async Task BrowseDirectoryAsync(Task<MediaDirectory> fetchMovies)
        {
            MediaDirectory directory = await fetchMovies.ConfigureAwait(false);
            builder.SetTotalMatchingItems(directory.TotalSize);
            foreach (MediaItem media in directory.Items.OfType<MediaItem>())
            {
                builder.AddEntry(this.CreateEntry(media));
            }
        }

        Task BrowseFirstCharacterAsync(char character) => sectionType switch
        {
            LibrarySectionType.Movie => BrowseDirectoryAsync(server.Library.ListMoviesByFirstCharacterAsync(sectionKey, character, parameters, cancellationToken)),
            LibrarySectionType.Show => BrowseDirectoryAsync(server.Library.ListTVShowsByFirstCharacterAsync(sectionKey, character, parameters, cancellationToken)),
            LibrarySectionType.Artist => BrowseDirectoryAsync(server.Library.ListMusicByFirstCharacterAsync(sectionKey, character, parameters, cancellationToken)),
            _ => Task.CompletedTask,
        };

        async Task BrowseFirstCharactersAsync() => Array.ForEach(
            await server.Library.ListFirstCharactersAsync(sectionKey, cancellationToken).ConfigureAwait(false),
            character => builder.AddEntry(new(
                char.ToString(character),
                ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Menu),
                BrowseIdentifier: $"library.{sectionKey}:{(int)sectionType}.firstCharacter.{character}"
            ))
        );

        Task BrowseRecentlyAddedAsync() => sectionType switch
        {
            LibrarySectionType.Movie => BrowseDirectoryAsync(server.Library.ListMoviesRecentlyAddedAsync(sectionKey, parameters, cancellationToken)),
            LibrarySectionType.Show => BrowseDirectoryAsync(server.Library.ListTVShowsRecentlyAddedAsync(sectionKey, parameters, cancellationToken)),
            LibrarySectionType.Artist => BrowseDirectoryAsync(server.Library.ListMusicRecentlyAddedAsync(sectionKey, parameters, cancellationToken)),
            _ => Task.CompletedTask
        };

        async Task BrowseSectionRootAsync()
        {
            LibrarySectionDetail detail = await server.Library.GetSectionDetailAsync(sectionKey, cancellationToken).ConfigureAwait(false);
            builder
                .SetTitle(detail.Title)
                .AddEntry(new(
                    $"All {Enum.GetName(sectionType)}s",
                    ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Menu),
                    BrowseIdentifier: $"library.{sectionKey}:{(int)sectionType}.all"
                ))
                .AddEntry(new(
                    $"By First Character",
                    ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Menu),
                    BrowseIdentifier: $"library.{sectionKey}:{(int)sectionType}.firstCharacter"
                ))
                .AddEntry(new(
                    "Recently Added",
                    ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Menu),
                    BrowseIdentifier: $"library.{sectionKey}:{(int)sectionType}.recentlyAdded"
                ));
        }
    }

    private async Task BrowsePlayersAsync(IPlexServer server, bool refresh, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        PlayerData[] players = await server.GetPlayersAsync(refresh, cancellationToken).ConfigureAwait(false);
        if (players.Length == 0)
        {
            builder.AddEntry(new(Title: "Clients not found!", Label: "Click to attempt reloading the list", BrowseIdentifier: "player.refresh"));
            return;
        }
        builder.AddHeader("Available Players");
        foreach (PlayerData player in players)
        {
            builder.AddEntry(new(
                player.Name,
                Label: player.IsOnline ? default : "(Offline)",
                ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Player),
                ActionIdentifier: $"player.{player.MachineIdentifier}",
                UIAction: DirectoryUIAction.Close
            ));
        }
    }

    private async Task BrowseRootAsync(IPlexServer server, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        builder
            .SetTitle("Plex")
            .AddTileRow([new(this.GetEmbeddedResourceUrl(EmbeddedImage.Logo))])
            .AddEntry(new(
                server.SelectedPlayerName is { } name ? $"Switch player ({name})" : "Select player",
                BrowseIdentifier: "player",
                ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Player)
            ));
        if (server is not { IsConnected: true, SelectedPlayer.IsOnline: true, Library: { } library })
        {
            return;
        }
        foreach (LibrarySection section in await library.ListSectionsAsync(cancellationToken).ConfigureAwait(false))
        {
            builder.AddEntry(new(
                section.Title,
                ThumbnailUri: this.GetEmbeddedResourceUrl(section.Type switch
                {
                    LibrarySectionType.Show => EmbeddedImage.TVShow,
                    LibrarySectionType.Movie => EmbeddedImage.Movie,
                    _ => EmbeddedImage.Music,
                }),
                BrowseIdentifier: $"library.{section.Key}:{(int)section.Type}"
            ));
        }
    }

    private DirectoryEntry CreateEntry(MediaItem media) => new(
        media.Title,
        media.Summary,
        ActionIdentifier: $"media.{media.RatingKey}",
        ThumbnailUri: media.ThumbnailUri is { } uri ? this.GetThumbnailUrl(uri, ImageSize.Small) : null
    );

    private string GetActiveMediaText(string machineIdentifier, Func<MediaItem, string> textProducer)
    {
        if (this.GetServer(machineIdentifier) is not { IsConnected: true } server)
        {
            return "Server not connected";
        }
        if (server.SelectedPlayer is not { } player)
        {
            return "Player not selected";
        }
        if (!player.IsOnline)
        {
            return "Player is offline";
        }
        return server.ActiveMedia switch
        {
            { } media => textProducer(media),
            null => "Nothing is playing",
        };
    }

    private string GetCoverArt(MediaItem? media) => media is { ThumbnailUri: { } uri }
        ? this.GetThumbnailUrl(uri)
        : this.GetEmbeddedResourceUrl(EmbeddedImage.Logo);

    private DiscoveredDevice[] GetDiscoveredServers(string? optionalMachineIdentifier)
    {
        return optionalMachineIdentifier switch
        {
            null => [.. serverManager.Data.Values.Select(ServerDiscoveryResult)],
            { Length: > 0 } id when serverManager.Data.TryGetValue(id, out ServerData data) => [ServerDiscoveryResult(data)],
            _ => []
        };

        DiscoveredDevice ServerDiscoveryResult(ServerData data) => new(data.MachineIdentifier, $"{data.Name} ({deviceName})");
    }

    private string GetEmbeddedResourceUrl(EmbeddedImage image)
    {
        string fileName = TextAttribute.GetText(image);
        return $"{this._uriPrefix}{VirtualDirectories.Embedded}/{fileName}";
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
            string imageUrl = PlexDeviceProviderBase.DecodeBase64(suffix);
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, new Uri(imageUrl)) { Headers = { { "X-Plex-Token", tokenStore.AuthToken } } };
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
            catch (HttpRequestException e)
            {
                Console.WriteLine(e);
                return new StatusCodeResult(503);
            }
        }
    }

    private bool IsConnected(string machineIdentifier) => this.GetServer(machineIdentifier) is { IsConnected: true };

    private Task NotifyInitialServersAsync(string[] machineIdentifiers, CancellationToken cancellationToken)
    {
        if (machineIdentifiers.Length != 0)
        {
            _ = Task.Run(InitializeUponStartup, cancellationToken);
        }
        return Task.CompletedTask;

        async Task InitializeUponStartup()
        {
            await startupTask.ConfigureAwait(false); // Wait for NEEO SDK to initialize
            await this.InitializeAsync(cancellationToken: default).ConfigureAwait(false);
            foreach (string machineIdentifier in machineIdentifiers)
            {
                await this.OnServerAddedAsync(machineIdentifier, cancellationToken: default).ConfigureAwait(false);
            }
        }
    }

    private async Task OnServerAddedAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (serverManager.GetServer(machineIdentifier) is not { } server)
        {
            logger.LogWarning("Plex device added but server not found: {Id}", machineIdentifier);
            return;
        }
        logger.LogInformation("Plex device added: {Name} ({IPAddress})", server.Name, server.ServerData.IPAddress);
        if (!this._servers.TryAdd(machineIdentifier, server) || this._notifier is not { } notifier)
        {
            return;
        }
        // Notify on changes to connection.
        server.IsConnectedChanged
            .Skip(1)
            .DistinctUntilChanged()
            .Select(isConnected => Observable.FromAsync((token) => notifier.SendPowerNotificationAsync(isConnected, machineIdentifier, token)))
            .Switch()
            .TakeUntil(server.Disposed)
            .Subscribe();
        // Notify on changes to player.
        server.SelectedPlayerChanged
            .Select(data => data is { Name: { } name } ? name : string.Empty)
            .DistinctUntilChanged()
            .Skip(1)
            .Select(playerName => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Player.SensorName, playerName, machineIdentifier, token)))
            .Switch()
            .TakeUntil(server.Disposed)
            .Subscribe();
        // Notify on changes to play state.
        server.PlayStateChanged
            .Select(state => state is PlayState.Playing or PlayState.Buffering)
            .DistinctUntilChanged()
            .Skip(1)
            .Select(isPlaying => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Playing.SensorName, isPlaying, machineIdentifier, token)))
            .Switch()
            .TakeUntil(server.Disposed)
            .Subscribe();
        // Notify on changes to active media.
        server.IsConnectedChanged
            .CombineLatest(server.SelectedPlayerChanged, server.ActiveMediaChanged)
            .Select(_ => this.GetTitle(machineIdentifier))
            .DistinctUntilChanged()
            .Skip(1)
            .Select(value => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Title.SensorName, value, machineIdentifier, token)))
            .Switch()
            .TakeUntil(server.Disposed)
            .Subscribe();
        server.IsConnectedChanged
            .CombineLatest(server.SelectedPlayerChanged, server.ActiveMediaChanged)
            .Select(_ => this.GetDescription(machineIdentifier))
            .DistinctUntilChanged()
            .Skip(1)
            .Select(value => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.Description.SensorName, value, machineIdentifier, token)))
            .Switch()
            .TakeUntil(server.Disposed)
            .Subscribe();
        server.IsConnectedChanged
            .CombineLatest(server.SelectedPlayerChanged, server.ActiveMediaChanged)
            .Select(_ => this.GetCoverArt(machineIdentifier))
            .DistinctUntilChanged()
            .Skip(1)
            .Select(value => Observable.FromAsync((token) => notifier.SendNotificationAsync(Components.CoverArt.SensorName, value, machineIdentifier, token)))
            .Switch()
            .TakeUntil(server.Disposed)
            .Subscribe();
        await server.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task OnServerRemovedAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (this._servers.TryRemove(machineIdentifier, out IPlexServer? server))
        {
            logger.LogInformation("Plex device removed: {Name}", machineIdentifier);
            server.Dispose();
        }
        return Task.CompletedTask;
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

    private enum EmbeddedImage
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
        public const string RootDirectory = "ROOT_DIRECTORY";

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

    private static class VirtualDirectories
    {
        public const string Embedded = "embedded";

        public const string Thumbnail = "thumbnail";
    }
}
