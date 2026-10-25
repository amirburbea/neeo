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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public abstract partial class PlexDeviceProviderBase(
    IHttpClientFactory httpClientFactory,
    IPlexServerManager serverManager,
    IPlexTokenStore tokenStore,
    IHostApplicationLifetime applicationLifetime,
    ILogger logger,
    DeviceType deviceType,
    string deviceName
) : IDeviceProvider, IDisposable
{
    private readonly Task _startupTask = PlexDeviceProviderBase.CreateStartupTask(applicationLifetime);

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
    private static readonly UrlTokenStore _imageUrlTokens = new();
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");
    private static readonly Uri _userUri = new($"https://plex.tv/api/v2/user");

    public static string RegisterImageUrl(string imageUrl) => PlexDeviceProviderBase._imageUrlTokens.Register(imageUrl);

    public static string? ResolveImageUrl(string token) => PlexDeviceProviderBase._imageUrlTokens.Resolve(token);

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
        .SetManufacturer("Plex");

    protected string GetCoverArt(string machineIdentifier) => this.GetCoverArt(this.GetServer(machineIdentifier)?.ActiveMedia);

    protected string GetDescription(string machineIdentifier) => this.GetActiveMediaText(
        machineIdentifier,
        media => media.Summary is { Length: > 0 } summary ? summary : media.Title
    );

    protected IPlexServer? GetServer(string machineIdentifier) => this._servers.GetValueOrDefault(machineIdentifier);

    protected string GetThumbnailUrl(Uri plexUri, ImageSize size = ImageSize.Large)
    {
        int pixels = size is ImageSize.Large ? 480 : 100;
        string imageUrl = $"{plexUri}&width={pixels}&height={pixels}";
        string token = PlexDeviceProviderBase.RegisterImageUrl(imageUrl);
        return $"{this._uriPrefix}{VirtualDirectories.Thumbnail}/{token}";
    }

    protected string GetTitle(string machineIdentifier) => this.GetActiveMediaText(machineIdentifier, static media => media.Title);

    protected async Task HandleDirectoryActionAsync(string machineIdentifier, string actionIdentifier, CancellationToken cancellationToken)
    {
        int index = actionIdentifier.IndexOf('.');
        if (index == -1 || this.GetServer(machineIdentifier) is not { } server)
        {
            return;
        }
        string suffix = actionIdentifier[(index + 1)..];
        Task actionTask = actionIdentifier[..index] switch
        {
            "media" when int.TryParse(suffix, out int ratingKey) => server.PlayMediaAsync(ratingKey, cancellationToken),
            "player" => server.SelectPlayerAsync(suffix, cancellationToken),
            _ => Task.CompletedTask,
        };
        await actionTask.ConfigureAwait(false);
    }

    protected async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await serverManager.InitializeAsync().ConfigureAwait(false);
    }

    private bool IsConnected(string machineIdentifier) => this.GetServer(machineIdentifier) is { IsConnected: true };

    protected bool IsPlaying(string machineIdentifier) => this.GetServer(machineIdentifier) is { PlayState: PlayState.Playing or PlayState.Buffering };

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

        Task BrowseAllAsync() => BrowseDirectoryAsync(server.Library.ListMediaAsync(sectionKey, parameters, cancellationToken));

        async Task BrowseDirectoryAsync(Task<MediaDirectory> fetchMovies)
        {
            MediaDirectory directory = await fetchMovies.ConfigureAwait(false);
            builder.SetTotalMatchingItems(directory.TotalSize);
            foreach (MediaItem media in directory.Items.OfType<MediaItem>())
            {
                builder.AddEntry(this.CreateEntry(media));
            }
        }

        Task BrowseFirstCharacterAsync(char character) => BrowseDirectoryAsync(server.Library.ListMediaByFirstCharacterAsync(sectionKey, character, parameters, cancellationToken));

        async Task BrowseFirstCharactersAsync() => Array.ForEach(
            await server.Library.ListFirstCharactersAsync(sectionKey, cancellationToken).ConfigureAwait(false),
            character => builder.AddEntry(new(
                char.ToString(character),
                ThumbnailUri: this.GetEmbeddedResourceUrl(EmbeddedImage.Menu),
                BrowseIdentifier: $"library.{sectionKey}:{(int)sectionType}.firstCharacter.{character}"
            ))
        );

        Task BrowseRecentlyAddedAsync() => BrowseDirectoryAsync(server.Library.ListMediaRecentlyAddedAsync(sectionKey, parameters, cancellationToken));

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
        ActionIdentifier: media.Type is MediaType.Episode or MediaType.Movie or MediaType.Video ? $"media.{media.RatingKey}" : null,
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

    private async Task<IResult> HandleHttpRequestAsync(HttpRequest request, string path, CancellationToken cancellationToken)
    {
        int index = path.IndexOf('/');
        if (index == -1)
        {
            return Results.NotFound();
        }
        string suffix = path[(index + 1)..];
        return path[..index] switch
        {
            VirtualDirectories.Embedded => await HandleEmbeddedImageRouteAsync(suffix).ConfigureAwait(false),
            VirtualDirectories.Thumbnail => await HandleThumbnailRouteAsync(suffix).ConfigureAwait(false),
            _ => Results.NotFound(),
        };

        static async Task<IResult> HandleEmbeddedImageRouteAsync(string fileName)
        {
            if (Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(PlexServer).Namespace}.Images.{fileName}") is not { } stream)
            {
                return Results.NotFound();
            }
            return Results.File(stream, PlexDeviceProviderBase.GetContentType(fileName), fileName);
        }

        async Task<IResult> HandleThumbnailRouteAsync(string suffix)
        {
            if (PlexDeviceProviderBase.ResolveImageUrl(suffix) is not { } imageUrl)
            {
                return Results.NotFound();
            }
            HttpResponseMessage? response = null;
            try
            {
                using HttpRequestMessage upstreamRequest = new(HttpMethod.Get, new Uri(imageUrl)) { Headers = { { "X-Plex-Token", tokenStore.AuthToken } } };
                response = await this._httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return Results.StatusCode((int)response.StatusCode);
                }
                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                string contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                // The response now backs the streamed result, so it is disposed with the request rather than below.
                request.HttpContext.Response.RegisterForDispose(response);
                response = null;
                return Results.Stream(stream, contentType);
            }
            catch (FormatException)
            {
                return Results.BadRequest();
            }
            catch (HttpRequestException e)
            {
                logger.LogWarning(e, "Failed to proxy Plex image from {Url}.", imageUrl);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private Task NotifyInitialServersAsync(string[] machineIdentifiers, CancellationToken cancellationToken)
    {
        if (machineIdentifiers.Length != 0)
        {
            _ = Task.Run(InitializeUponStartup, cancellationToken);
        }
        return Task.CompletedTask;

        // Runs unobserved in the background, so every failure must be handled (and logged) here.
        async Task InitializeUponStartup()
        {
            try
            {
                await this._startupTask.ConfigureAwait(false); // Wait for NEEO SDK to initialize
                await this.InitializeAsync(cancellationToken: default).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to initialize the Plex servers.");
                return;
            }
            foreach (string machineIdentifier in machineIdentifiers)
            {
                try
                {
                    await this.OnServerAddedAsync(machineIdentifier, cancellationToken: default).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to initialize Plex server {MachineIdentifier}.", machineIdentifier);
                }
            }
        }
    }

    private static Task CreateStartupTask(IHostApplicationLifetime applicationLifetime)
    {
        if (applicationLifetime.ApplicationStarted.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }
        TaskCompletionSource taskCompletionSource = new();
        applicationLifetime.ApplicationStarted.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), taskCompletionSource);
        return taskCompletionSource.Task;
    }

    private async Task OnServerAddedAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (serverManager.GetServer(machineIdentifier) is not { } server)
        {
            logger.LogWarning("Plex device added but server not found: {Id}", machineIdentifier);
            return;
        }
        logger.LogInformation("Plex device added: {Name} ({IPAddress})", server.Name, server.ServerData.IPAddress);
        if (!this._servers.TryAdd(machineIdentifier, server))
        {
            server.Dispose();
            return;
        }
        if (this._notifier is not { } notifier)
        {
            return;
        }
        // Notify on changes to connection.
        Task SendPowerAsync(bool isConnected, CancellationToken token) => notifier.SendPowerNotificationAsync(isConnected, machineIdentifier, token);
        PlexDeviceProviderBase.NotifyOnChange(server.IsConnectedChanged, server.Disposed, SendPowerAsync, logger, "Power");
        if (server.IsConnected)
        {
            // The underlying server is shared with the other Plex provider, so it may already be connected (no change will be reported).
            await PlexDeviceProviderBase.SendSafelyAsync(SendPowerAsync, true, logger, "Power", cancellationToken).ConfigureAwait(false);
        }
        // Notify on changes to player.
        PlexDeviceProviderBase.NotifyOnChange(
            server.SelectedPlayerChanged.Select(static data => data is { Name: { } name } ? name : string.Empty),
            server.Disposed,
            (playerName, token) => notifier.SendNotificationAsync(Components.Player.SensorName, playerName, machineIdentifier, token),
            logger,
            Components.Player.SensorName
        );
        // Notify on changes to play state.
        PlexDeviceProviderBase.NotifyOnChange(
            server.PlayStateChanged.Select(static state => state is PlayState.Playing or PlayState.Buffering),
            server.Disposed,
            (isPlaying, token) => notifier.SendNotificationAsync(Components.Playing.SensorName, isPlaying, machineIdentifier, token),
            logger,
            Components.Playing.SensorName
        );
        // Notify on changes to active media.
        IObservable<Unit> mediaChanged = server.IsConnectedChanged.CombineLatest(server.SelectedPlayerChanged, server.ActiveMediaChanged).Select(static _ => Unit.Default);
        PlexDeviceProviderBase.NotifyOnChange(
            mediaChanged.Select(_ => this.GetTitle(machineIdentifier)),
            server.Disposed,
            (value, token) => notifier.SendNotificationAsync(Components.Title.SensorName, value, machineIdentifier, token),
            logger,
            Components.Title.SensorName
        );
        PlexDeviceProviderBase.NotifyOnChange(
            mediaChanged.Select(_ => this.GetDescription(machineIdentifier)),
            server.Disposed,
            (value, token) => notifier.SendNotificationAsync(Components.Description.SensorName, value, machineIdentifier, token),
            logger,
            Components.Description.SensorName
        );
        PlexDeviceProviderBase.NotifyOnChange(
            mediaChanged.Select(_ => this.GetCoverArt(machineIdentifier)),
            server.Disposed,
            (value, token) => notifier.SendNotificationAsync(Components.CoverArt.SensorName, value, machineIdentifier, token),
            logger,
            Components.CoverArt.SensorName
        );
        await server.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a notification each time <paramref name="values"/> changes (ignoring the initial value), until <paramref name="until"/> fires.
    /// A newer value supersedes one still being sent. A failed send is logged and later values are still sent; an error in the
    /// <paramref name="values"/> stream itself is logged rather than left unhandled (which would terminate the process).
    /// </summary>
    internal static IDisposable NotifyOnChange<TValue>(
        IObservable<TValue> values,
        IObservable<Unit> until,
        Func<TValue, CancellationToken, Task> send,
        ILogger logger,
        string description
    ) => values
        .DistinctUntilChanged()
        .Skip(1)
        .Select(value => Observable.FromAsync(token => PlexDeviceProviderBase.SendSafelyAsync(send, value, logger, description, token)))
        .Switch()
        .TakeUntil(until)
        .Subscribe(
            static _ => { },
            exception => logger.LogError(exception, "Plex '{Description}' notifications stopped unexpectedly.", description)
        );

    private static async Task SendSafelyAsync<TValue>(Func<TValue, CancellationToken, Task> send, TValue value, ILogger logger, string description, CancellationToken cancellationToken)
    {
        try
        {
            await send(value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Superseded by a newer value, or the server was disposed.
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to send Plex '{Description}' notification.", description);
        }
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

    internal async Task<bool> QueryIsRegisteredAsync(CancellationToken cancellationToken)
    {
        // Check if token exists, we have not previously registered if it doesn't.
        if (string.IsNullOrEmpty(tokenStore.AuthToken))
        {
            return false;
        }
        switch (await GetTokenStatusAsync(cancellationToken).ConfigureAwait(false))
        {
            case HttpStatusCode.OK:
                return true;
            case HttpStatusCode.Unauthorized:
                // Plex has rejected the token: clear it so the user is asked to register again.
                tokenStore.AuthToken = null;
                return false;
            default:
                // Plex couldn't be reached or failed to answer. The token may well still be valid, so keep it (clearing it would
                // force the user to sign in again after every outage) and don't report the device as unregistered.
                logger.LogWarning("Could not verify the Plex token with plex.tv; keeping it.");
                return true;
        }

        async Task<HttpStatusCode?> GetTokenStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await this._httpClient.HeadAsync(
                    PlexDeviceProviderBase._userUri,
                    request => request.Headers.Add("X-Plex-Token", tokenStore.AuthToken),
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to reach plex.tv to verify the Plex token.");
                return null;
            }
        }
    }

    internal async Task<RegistrationResult> RegisterAsync(string userName, string password, CancellationToken cancellationToken)
    {
        try
        {
            JsonElement element = await this._httpClient.PostAsync(
                PlexDeviceProviderBase._signInUri,
                PlexJsonSerializerContext.Default.JsonElement,
                configureRequest: ConfigureRequest,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            tokenStore.AuthToken = element.GetProperty("user").GetProperty("authToken").GetString()!;
            return RegistrationResult.Success;
        }
        catch (HttpRequestException ex) when (ex.StatusCode is { } code && Enum.GetName(code) is { } message)
        {
            return RegistrationResult.Failed(message);
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
