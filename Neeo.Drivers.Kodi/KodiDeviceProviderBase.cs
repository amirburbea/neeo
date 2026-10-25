using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.Kodi.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

public abstract partial class KodiDeviceProviderBase : IDeviceProvider, IDisposable
{
    private static readonly Dictionary<Buttons, Func<KodiClient, CancellationToken, Task>> _buttonFunctions = new()
    {
        { Buttons.Back, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Back, cancellationToken) },
        { Buttons.ChannelDown, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ChannelDown, cancellationToken) },
        { Buttons.ChannelUp, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ChannelUp, cancellationToken) },
        { Buttons.CursorDown, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Down, cancellationToken) },
        { Buttons.CursorEnter, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Select, cancellationToken) },
        { Buttons.CursorLeft, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Left, cancellationToken) },
        { Buttons.CursorRight, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Right, cancellationToken) },
        { Buttons.CursorUp, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Up, cancellationToken) },
        { Buttons.Digit0, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number0, cancellationToken) },
        { Buttons.Digit1, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number1, cancellationToken) },
        { Buttons.Digit2, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number2, cancellationToken) },
        { Buttons.Digit3, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number3, cancellationToken) },
        { Buttons.Digit4, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number4, cancellationToken) },
        { Buttons.Digit5, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number5, cancellationToken) },
        { Buttons.Digit6, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number6, cancellationToken) },
        { Buttons.Digit7, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number7, cancellationToken) },
        { Buttons.Digit8, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number8, cancellationToken) },
        { Buttons.Digit9, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number9, cancellationToken) },
        { Buttons.DigitSeparator, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.NumberSeperator, cancellationToken) },
        { Buttons.Exit, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Back, cancellationToken) },
        { Buttons.Home, (client, cancellationToken) => client.GoHomeAsync(cancellationToken) },
        { Buttons.Info, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Info, cancellationToken) },
        { Buttons.Language, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Language, cancellationToken) },
        { Buttons.Menu, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Menu, cancellationToken) },
        { Buttons.MuteToggle, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.MuteToggle, cancellationToken) },
        { Buttons.Next, (client, cancellationToken) => client.SendGoToCommandAsync(next: true, cancellationToken) },
        { Buttons.NextTrack, (client, cancellationToken) => client.SendGoToCommandAsync(next: true, cancellationToken) },
        { Buttons.Pause, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Pause, cancellationToken) },
        { Buttons.Play, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Play, cancellationToken) },
        { Buttons.PlayToggle, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Pause, cancellationToken) },
        { Buttons.PlayPauseToggle, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Pause, cancellationToken) },
        { Buttons.Previous, (client, cancellationToken) => client.SendGoToCommandAsync(next: false, cancellationToken) },
        { Buttons.PreviousTrack, (client, cancellationToken) => client.SendGoToCommandAsync(next: false, cancellationToken) },
        { Buttons.SkipBackward, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.SkipPrevious, cancellationToken) },
        { Buttons.SkipForward, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.SkipNext, cancellationToken) },
        { Buttons.Stop, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Stop, cancellationToken) },
        { Buttons.Subtitle, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Subtitle, cancellationToken) },
        { Buttons.VolumeDown, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.VolumeDown, cancellationToken) },
        { Buttons.VolumeUp, (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.VolumeUp, cancellationToken) },
    };

    private static readonly Dictionary<string, Func<KodiClient, CancellationToken, Task>> _customButtonFunctions = new(StringComparer.Ordinal)
    {
        { "CHANNEL SEARCH", (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ChannelSearch, cancellationToken) },
        { "FAVORITE", (client, cancellationToken) => client.ActivateWindowAsync("pvrosdchannels", cancellationToken) },
        { "OSD", (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.OnScreenDisplay, cancellationToken) },
        { "PLAY PAUSE", (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.PlayPauseToggle, cancellationToken) },
        { "TOGGLE FULLSCREEN", (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ToggleFullScreen, cancellationToken) },
        { "Scan Video Library", (client, cancellationToken) => client.ScanVideoLibraryAsync(cancellationToken) },
        { "Scan Audio Library", (client, cancellationToken) => client.ScanAudioLibraryAsync(cancellationToken) },
    };

    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    private readonly KodiClientManager _clientManager;
    private readonly Lazy<IDeviceBuilder> _deviceBuilder;
    private readonly ConcurrentDictionary<string, byte> _deviceIds = new(StringComparer.Ordinal);
    private readonly string _deviceName;
    private readonly DeviceType _deviceType;
    private readonly ILogger _logger;
    private string _uriPrefix = string.Empty;

    protected KodiDeviceProviderBase(KodiClientManager clientManager, string deviceName, DeviceType deviceType, ILogger logger)
    {
        this._deviceBuilder = new(this.CreateDevice);
        this._clientManager = clientManager;
        this._deviceName = deviceName;
        this._deviceType = deviceType;
        this._logger = logger;
        this._clientManager.ClientAdded += this.ClientManager_ClientAdded;
    }

    public IDeviceBuilder DeviceBuilder => this._deviceBuilder.Value;

    /// <summary>
    /// Gets or sets the notifier directly (test seam - normally set by the SDK when the server starts).
    /// </summary>
    internal IDeviceNotifier? Notifier { get; set; }

    public virtual void Dispose()
    {
        this._clientManager.ClientAdded -= this.ClientManager_ClientAdded;
        foreach (KodiClient client in this._clientManager.Clients)
        {
            // So that shutting down does not report every Kodi as powered off.
            this.DetachEventHandlers(client);
        }
        GC.SuppressFinalize(this);
    }

    protected static bool IsClientReady(KodiClient client)
    {
        if (client.IsConnected)
        {
            return true;
        }
        client.RequestReconnect();
        return false;
    }

    protected virtual IDeviceBuilder CreateDevice() => Device.Create(this._deviceName, this._deviceType)
        .SetManufacturer(nameof(Kodi))
        .SetSpecificName(this._deviceName)
        .AddAdditionalSearchTokens(nameof(Kodi), "XBMC")
        .AddButtonHandler(this.HandleButtonAsync)
        .AddDirectory("Library", "Library", default, this.PopulateRootDirectoryAsync, this.HandleDirectoryActionAsync)
        .AddDirectory("MovieLibrary", "Movies", default, this.PopulateRootDirectoryAsync, this.HandleDirectoryActionAsync, ".movies")
        .AddDirectory("MusicLibrary", "Music", default, this.PopulateRootDirectoryAsync, this.HandleDirectoryActionAsync, ".music")
        .AddDirectory("TVShowLibrary", "TV Shows", default, this.PopulateRootDirectoryAsync, this.HandleDirectoryActionAsync, ".tvshows")
        .AddDirectory("PvrLibrary", "PVR", default, this.PopulateRootDirectoryAsync, this.HandleDirectoryActionAsync, ".pvr")
        .AddDirectory("QUEUE", "Queue", DirectoryRole.Queue, this.PopulateQueueDirectoryAsync, this.HandleDirectoryActionAsync, ".queue")
        .AddPowerStateSensor(this.IsClientReady)
        .EnableDeviceRoute(this.SetUriPrefix, this.HandleDeviceRouteAsync)
        .EnableDiscovery(Constants.DiscoveryHeader, Constants.DiscoveryDescription, this.DiscoverAsync)
        .EnableRegistration(Constants.RegistrationHeader, Constants.RegistrationDescription, _ => Task.FromResult(this._clientManager.HasCredentials), this.RegisterAsync)
        .EnableNotifications(notifier => this.Notifier = notifier)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, this.InitializeDeviceListAsync)
        .RegisterInitializer(cancellationToken => this._clientManager.InitializeAsync(cancellationToken: cancellationToken));

    protected KodiClient? GetClientOrDefault(string deviceId) => this._clientManager.GetClientOrDefault(deviceId);

    protected TValue GetClientValue<TValue>(
        string deviceId,
        Func<KodiClient, TValue> projection,
        TValue defaultValue
    ) => this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) ? projection(client) : defaultValue;

    protected string GetCoverArt(string deviceId) => this.GetClientValue(
        deviceId,
        client => this.GetThumbnailUrl(client, client.PlayerState.NowPlayingImage, ImageSize.Large),
        string.Empty
    );

    protected async Task<RegistrationResult> RegisterAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RegistrationResult.Failed("Username cannot be empty.");
        }
        if (!await this._clientManager.ValidateCredentialsAsync(userName, password, cancellationToken).ConfigureAwait(false))
        {
            return RegistrationResult.Failed("Could not authenticate with Kodi. Please check your username and password.");
        }
        this._clientManager.SetCredentials(userName, password);
        return RegistrationResult.Success;
    }

    protected string GetDescription(string deviceId) => this.GetClientValue(
        deviceId,
        static client => client.PlayerState.NowPlayingDescription,
        string.Empty
    );

    protected abstract string GetDisplayName(KodiClient client);

    protected bool GetIsMuted(string deviceId) => this.GetClientValue(
        deviceId,
        static client => client.IsMuted,
        default
    );

    protected bool GetIsPlaying(string deviceId) => this.GetClientValue(
        deviceId,
        static client => client.PlayerState.PlayState == PlayState.Playing,
        default
    );

    protected string GetTitle(string deviceId) => this.GetClientValue(
        deviceId,
        static client => client.PlayerState.NowPlayingLabel,
        string.Empty
    );

    protected double GetVolume(string deviceId) => this.GetClientValue(
        deviceId,
        static client => (double)client.Volume,
        default
    );

    protected async Task HandleDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) && KodiDeviceProviderBase.TryTranslate(actionIdentifier) is { Key: { } key, Value: { } value })
        {
            await client.OpenFileAsync(key, value, cancellationToken).ConfigureAwait(false);
        }
    }

    protected Task PopulateQueueDirectoryAsync(string deviceId, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is not { } client || !KodiDeviceProviderBase.IsClientReady(client))
        {
            builder.AddEntry(new("Kodi is not connected!", "Try reloading the list.", ThumbnailUri: Images.Kodi, UIAction: DirectoryUIAction.Reload));
        }
        else
        {
            builder.AddEntry(new("Queue is empty", "No items in current playlist.", ThumbnailUri: Images.Kodi));
        }
        return Task.CompletedTask;
    }

    protected async Task PopulateRootDirectoryAsync(string deviceId, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is not { } client || !KodiDeviceProviderBase.IsClientReady(client))
        {
            builder.AddEntry(new("Kodi is not connected!", "Click to attempt reloading the list", ThumbnailUri: Images.Kodi, UIAction: DirectoryUIAction.Reload));
            return;
        }
        EmbeddedImages images = new(this._uriPrefix);
        string identifier = builder.Parameters.BrowseIdentifier ?? string.Empty;
        int offset = builder.Parameters.Offset;
        int limit = builder.Parameters.Limit;
        switch (identifier)
        {
            case "":
                KodiDeviceProviderBase.PopulateLibraryRoot(builder, images);
                break;
            case ".movies":
                KodiDeviceProviderBase.PopulateMoviesLibraryRoot(builder, images);
                break;
            case ".movies.movies":
                await this.PopulateMoviesLibraryAsync(client, builder, offset, limit).ConfigureAwait(false);
                break;
            case ".movies.inprogress":
                await this.PopulateMoviesLibraryAsync(client, builder, offset, limit, new("inprogress", "true")).ConfigureAwait(false);
                break;
            case ".movies.unwatched":
                await this.PopulateMoviesLibraryAsync(client, builder, offset, limit, new("playcount", "0")).ConfigureAwait(false);
                break;
            case ".movies.watched":
                await this.PopulateMoviesLibraryAsync(client, builder, offset, limit, new("playcount", "0", FilterOperator.GreaterThan)).ConfigureAwait(false);
                break;
            case ".movies.recent":
                await this.PopulateRecentMoviesLibraryAsync(client, builder, offset, limit, cancellationToken).ConfigureAwait(false);
                break;
            case ".music":
                KodiDeviceProviderBase.PopulateMusicLibraryRoot(builder, images);
                break;
            case ".music.albums":
                await this.PopulateAlbumLibraryAsync(client, builder, offset, limit, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case ".music.albums.recent":
                await this.PopulateRecentAlbumsLibraryAsync(client, builder, offset, limit, cancellationToken).ConfigureAwait(false);
                break;
            case ".music.artists":
                await this.PopulateArtistsLibraryAsync(client, builder, offset, limit, cancellationToken).ConfigureAwait(false);
                break;
            case ".tvshows":
                KodiDeviceProviderBase.PopulateTVShowsLibraryRoot(builder, images);
                break;
            case ".tvshows.tvshows":
                await this.PopulateTVShowsLibraryAsync(client, builder, offset, limit, cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case ".tvshows.recent":
                await this.PopulateRecentEpisodesLibraryAsync(client, builder, offset, limit, cancellationToken).ConfigureAwait(false);
                break;
            case ".pvr":
                KodiDeviceProviderBase.PopulatePvrLibraryRoot(builder, images);
                break;
            case ".pvr.tvchannels":
            case ".pvr.radiostations":
                break;
            default:
                switch (KodiDeviceProviderBase.TryTranslate(identifier))
                {
                    case { Key: "tvshowid", Value: int value, Suffix: { } suffix }:
                        await this.PopulateEpisodesLibraryAsync(client, builder, offset, limit, value, suffix).ConfigureAwait(false);
                        break;
                    case { Key: "artistid", Value: int value, Suffix: { } suffix }:
                        await this.PopulateAlbumLibraryAsync(client, builder, offset, limit, value, suffix, cancellationToken).ConfigureAwait(false);
                        break;
                    case { Key: "albumid", Value: int value, Suffix: { } suffix }:
                        await this.PopulateSongsLibraryAsync(client, builder, offset, limit, value, suffix, cancellationToken).ConfigureAwait(false);
                        break;
                }
                break;
        }
    }

    protected async Task SetVolumeAsync(string deviceId, double value, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client))
        {
            await client.SetVolumeAsync((int)value, cancellationToken).ConfigureAwait(false);
        }
    }

    private DirectoryEntry CreateListEntry(KodiClient client, IMediaInfo media, bool isAction = true, string? suffix = default)
    {
        string id = suffix == null ? media.GetId() : $"{media.GetId()}:{suffix}";
        return new(
            media.GetTitle(),
            media.GetDescription(),
            BrowseIdentifier: isAction ? null : id,
            ActionIdentifier: isAction ? id : null,
            ThumbnailUri: this.GetThumbnailUrl(client, media.GetCoverArt(), ImageSize.Small)
        );
    }

    private static string GetContentType(string fileName) => KodiDeviceProviderBase._contentTypeProvider.TryGetContentType(fileName, out string? contentType)
        ? contentType
        : "application/octet-stream";

    internal string GetThumbnailUrl(KodiClient client, string? coverArt, ImageSize size = ImageSize.Large)
    {
        if (string.IsNullOrEmpty(coverArt) || coverArt == Images.Kodi || string.IsNullOrEmpty(this._uriPrefix))
        {
            return Images.Kodi;
        }
        if (coverArt.StartsWith(this._uriPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return coverArt;
        }
        string rawUrl = Uri.TryCreate(coverArt, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? $"{uri.Scheme}://{uri.Authority}{uri.PathAndQuery}"
            : client.GetRawImageUrl(coverArt);
        int maxDimension = size == ImageSize.Small ? 120 : 480;
        string token = this._clientManager.RegisterImageUrl(rawUrl);
        return $"{this._uriPrefix}thumbnail/{maxDimension}/{token}";
    }

    private async Task<IResult> HandleDeviceRouteAsync(HttpRequest request, string path, CancellationToken cancellationToken) =>
        path.StartsWith("thumbnail/", StringComparison.OrdinalIgnoreCase)
            ? await this.HandleThumbnailRouteAsync(path[10..], cancellationToken).ConfigureAwait(false)
            : KodiDeviceProviderBase.HandleEmbeddedImageRoute(path);

    private static IResult HandleEmbeddedImageRoute(string path)
    {
        if (Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(KodiClient).Namespace}.Images.{path.Replace('/', '.')}") is not { } stream)
        {
            return Results.NotFound();
        }
        string fileDownloadName = path.LastIndexOf('/') is int index and not -1 ? path[(index + 1)..] : path;
        return Results.File(stream, KodiDeviceProviderBase.GetContentType(fileDownloadName), fileDownloadName);
    }

    private async Task<IResult> HandleThumbnailRouteAsync(string suffix, CancellationToken cancellationToken)
    {
        int maxDimension = 480;
        string token = suffix;
        int slashIndex = suffix.IndexOf('/');
        if (slashIndex != -1 && int.TryParse(suffix[..slashIndex], out int parsedDimension))
        {
            maxDimension = parsedDimension;
            token = suffix[(slashIndex + 1)..];
        }
        if (this._clientManager.ResolveImageUrl(token) is not { } imageUrl)
        {
            return Results.NotFound();
        }
        return await this._clientManager.FetchImageAsync(imageUrl, maxDimension, cancellationToken).ConfigureAwait(false) is ({ } bytes, { } contentType)
            ? Results.Bytes(bytes, contentType)
            : Results.NotFound();
    }

    [GeneratedRegex("^(?<key>[a-z]+)[:](?<id>[\\d]+)([:](?<suffix>.+))?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex IdentifierRegex();

    private async Task PopulateAlbumLibraryAsync(
        KodiClient client,
        IDirectoryBuilder builder,
        int offset,
        int limit,
        int? artistId = default,
        string? artistLabel = default,
        CancellationToken cancellationToken = default
    )
    {
        int end = offset + limit;
        (int total, AlbumInfo[] albums) = await client.GetAlbumsAsync(offset, end, artistId, cancellationToken).ConfigureAwait(false);
        if (offset == 0)
        {
            builder
                .AddHeader(artistLabel ?? "Albums")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
                );
        }
        foreach (AlbumInfo album in albums)
        {
            builder.AddEntry(this.CreateListEntry(client, album, isAction: false, suffix: album.GetTitle()));
        }
        builder.SetTotalMatchingItems(total);
    }

    private async Task PopulateRecentAlbumsLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, CancellationToken cancellationToken = default)
    {
        int end = offset + limit;
        (int total, AlbumInfo[] albums) = await client.GetRecentlyAddedAlbumsAsync(offset, end, cancellationToken).ConfigureAwait(false);
        if (offset == 0)
        {
            builder
                .AddHeader("Albums - Recent")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
                );
        }
        foreach (AlbumInfo album in albums)
        {
            builder.AddEntry(this.CreateListEntry(client, album, isAction: false, suffix: album.GetTitle()));
        }
        builder.SetTotalMatchingItems(total);
    }

    private async Task PopulateArtistsLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, CancellationToken cancellationToken = default)
    {
        int end = offset + limit;
        (int total, ArtistInfo[] artists) = await client.GetArtistsAsync(offset, end, cancellationToken).ConfigureAwait(false);
        if (offset == 0)
        {
            builder
                .AddHeader("Artists")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
                );
        }
        foreach (ArtistInfo artist in artists)
        {
            builder.AddEntry(this.CreateListEntry(client, artist, isAction: false, suffix: artist.GetTitle()));
        }
        builder.SetTotalMatchingItems(total);
    }

    private async Task PopulateSongsLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, int albumId, string? albumLabel = default, CancellationToken cancellationToken = default)
    {
        int end = offset + limit;
        (int total, SongInfo[] songs) = await client.GetSongsAsync(offset, end, albumId, cancellationToken).ConfigureAwait(false);
        if (offset == 0)
        {
            if (!string.IsNullOrEmpty(albumLabel))
            {
                builder.AddHeader(albumLabel);
            }
            builder.AddButtonRow(
                new("Play Album", ActionIdentifier: $"albumid:{albumId}", Inverse: false),
                new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
            );
        }
        foreach (SongInfo song in songs)
        {
            builder.AddEntry(this.CreateListEntry(client, song));
        }
        builder.SetTotalMatchingItems(total);
    }

    private async Task PopulateRecentMoviesLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, CancellationToken cancellationToken = default)
    {
        int end = offset + limit;
        (int total, VideoInfo[] videos) = await client.GetRecentlyAddedMoviesAsync(offset, end, cancellationToken).ConfigureAwait(false);
        if (offset == 0)
        {
            builder
                .AddHeader("Movies - Recent")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
                );
        }
        foreach (VideoInfo video in videos)
        {
            builder.AddEntry(this.CreateListEntry(client, video));
        }
        builder.SetTotalMatchingItems(total);
    }

    private async Task PopulateRecentEpisodesLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, CancellationToken cancellationToken = default)
    {
        int end = offset + limit;
        (int total, EpisodeInfo[] episodes) = await client.GetRecentlyAddedEpisodesAsync(offset, end, cancellationToken).ConfigureAwait(false);
        if (offset == 0)
        {
            builder
                .AddHeader("TV Shows - Recent")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
                );
        }
        foreach (EpisodeInfo episode in episodes)
        {
            builder.AddEntry(this.CreateListEntry(client, episode));
        }
        builder.SetTotalMatchingItems(total);
    }

    private async Task PopulateEpisodesLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, int? tvShowId = default, string? tvShowLabel = default)
    {
        int end = offset + limit;
        (int total, EpisodeInfo[] episodes) = await client.GetEpisodesAsync(offset, end, tvShowId).ConfigureAwait(false);
        if (offset == 0 && builder.BrowseIdentifier is { })
        {
            if (!string.IsNullOrEmpty(tvShowLabel))
            {
                builder.AddHeader(tvShowLabel);
            }
            builder.AddButtonRow(
                new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
            );
        }
        foreach (EpisodeInfo episode in episodes)
        {
            builder.AddEntry(this.CreateListEntry(client, episode));
        }
        builder.SetTotalMatchingItems(total);
    }

    private static void PopulateLibraryRoot(IDirectoryBuilder list, EmbeddedImages images) => list
        .AddTileRow(new DirectoryTile(Images.Kodi))
        .AddEntry(new("Movies", ThumbnailUri: images.Movie, BrowseIdentifier: ".movies"))
        .AddEntry(new("Music", ThumbnailUri: images.Music, BrowseIdentifier: ".music"))
        .AddEntry(new("TV Shows", ThumbnailUri: images.TVShow, BrowseIdentifier: ".tvshows"))
        .AddEntry(new("PVR", ThumbnailUri: images.Pvr, BrowseIdentifier: ".pvr"));

    private async Task PopulateMoviesLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, Filter? filter = default)
    {
        int end = offset + limit;
        (int total, VideoInfo[] videos) = await client.GetMoviesAsync(offset, end, filter).ConfigureAwait(false);
        if (offset == 0 && builder.BrowseIdentifier is { } id)
        {
            builder
                .AddHeader(id == ".movies" ? "Movies" : $"Movies ({id[8..]})")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close)
                );
        }
        foreach (VideoInfo video in videos)
        {
            builder.AddEntry(this.CreateListEntry(client, video));
        }
        builder.SetTotalMatchingItems(total);
    }

    private static void PopulateMoviesLibraryRoot(IDirectoryBuilder list, EmbeddedImages images) => list
        .AddHeader("Movies")
        .AddEntry(new("Movies", ThumbnailUri: images.Movie, BrowseIdentifier: ".movies.movies"))
        .AddEntry(new("Movies - In Progress", ThumbnailUri: images.Movie, BrowseIdentifier: ".movies.inprogress"))
        .AddEntry(new("Movies - Unwatched", ThumbnailUri: images.Movie, BrowseIdentifier: ".movies.unwatched"))
        .AddEntry(new("Movies - Watched", ThumbnailUri: images.Movie, BrowseIdentifier: ".movies.watched"))
        .AddEntry(new("Movies - Recent", ThumbnailUri: images.Movie, BrowseIdentifier: ".movies.recent"));

    private static void PopulateMusicLibraryRoot(IDirectoryBuilder list, EmbeddedImages images) => list
        .AddHeader("Music")
        .AddEntry(new("Albums", ThumbnailUri: images.Music, BrowseIdentifier: ".music.albums"))
        .AddEntry(new("Albums - Recent", ThumbnailUri: images.Music, BrowseIdentifier: ".music.albums.recent"))
        .AddEntry(new("Artists", ThumbnailUri: images.Music, BrowseIdentifier: ".music.artists"));

    private static void PopulatePvrLibraryRoot(IDirectoryBuilder list, EmbeddedImages images) => list
        .AddHeader("PVR")
        .AddEntry(new("TV Channels", ThumbnailUri: images.Pvr, BrowseIdentifier: ".pvr.tvchannels"))
        .AddEntry(new("Radio Stations", ThumbnailUri: images.Pvr, BrowseIdentifier: ".pvr.radiostations"));

    private async Task PopulateTVShowsLibraryAsync(KodiClient client, IDirectoryBuilder builder, int offset, int limit, Filter? filter = default, CancellationToken cancellationToken = default)
    {
        int end = offset + limit;
        (int total, TVShowInfo[] shows) = await client.GetTVShowsAsync(offset, end, filter, cancellationToken).ConfigureAwait(false);
        if (offset == 0 && builder.BrowseIdentifier is { } id)
        {
            builder
                .AddHeader(id == ".tvshows" ? "TV Shows" : $"TV Shows ({id[9..]})")
                .AddButtonRow(
                    new("Library", ActionIdentifier: ".root", Inverse: true, UIAction: DirectoryUIAction.GoToRoot),
                    new("Close", ActionIdentifier: ".close", Inverse: true, UIAction: DirectoryUIAction.Close));
        }
        foreach (TVShowInfo show in shows)
        {
            builder.AddEntry(this.CreateListEntry(client, show, isAction: false, suffix: show.GetDescription()));
        }
        builder.SetTotalMatchingItems(total);
    }

    private static void PopulateTVShowsLibraryRoot(IDirectoryBuilder list, EmbeddedImages images) => list
        .AddHeader("TV Shows")
        .AddEntry(new("TV Shows", ThumbnailUri: images.TVShow, BrowseIdentifier: ".tvshows.tvshows"))
        .AddEntry(new("TV Shows - Recent", ThumbnailUri: images.TVShow, BrowseIdentifier: ".tvshows.recent"));

    private static Identifier? TryTranslate(string identifier) => KodiDeviceProviderBase.IdentifierRegex().Match(identifier) is { Success: true, Groups: { } groups }
        ? new(groups["key"].Value, int.Parse(groups["id"].Value), groups["suffix"].Value)
        : null;

    internal void AddDeviceId(string deviceId) => this._deviceIds.TryAdd(deviceId, default);

    private void AttachEventHandlers(KodiClient client)
    {
        this.DetachEventHandlers(client);
        client.Connected += this.Client_Connected;
        client.Disconnected += this.Client_Disconnected;
        client.Error += this.Client_Error;
        client.PlayerStateChanged += this.Client_PlayerStateChanged;
        client.VolumeChanged += this.Client_VolumeChanged;
    }

    private void Client_Connected(object? sender, EventArgs e) => _ = this.HandleClientConnectedAsync(sender);

    private void Client_Disconnected(object? sender, EventArgs e) => _ = this.HandleClientDisconnectedAsync(sender);

    internal async Task HandleClientConnectedAsync(object? sender)
    {
        if (sender is not KodiClient client)
        {
            return;
        }
        try
        {
            if (this.Notifier is { } notifier)
            {
                await notifier.SendPowerNotificationAsync(true, client.DeviceId).ConfigureAwait(false);
            }
            await client.RefreshStateAsync().ConfigureAwait(false);
            await this.NotifyConnectedAsync(client).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Kodi connected handling for {DeviceId} failed.", client.DeviceId);
        }
    }

    internal async Task HandleClientDisconnectedAsync(object? sender)
    {
        if (sender is not KodiClient client || this.Notifier is not { } notifier)
        {
            return;
        }
        try
        {
            await notifier.SendPowerNotificationAsync(false, client.DeviceId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Kodi disconnected handling for {DeviceId} failed.", client.DeviceId);
        }
    }

    private void Client_Error(object? sender, DataEventArgs<string> e) => this._logger.LogWarning("Client {deviceId} error encountered: {error}.", ((KodiClient)sender!).DeviceId, e.Data);

    private void Client_PlayerStateChanged(object? sender, DataEventArgs<PlayerState> e) => _ = this.HandlePlayerStateChangedAsync(sender, e.Data);

    internal async Task HandlePlayerStateChangedAsync(object? sender, PlayerState state)
    {
        if (sender is not KodiClient client || this.Notifier is not { } notifier)
        {
            return;
        }
        try
        {
            string deviceId = client.DeviceId;
            Task[] tasks = state.PlayState is PlayState.Paused
                ? [
                    notifier.SendNotificationAsync("PLAYING_SENSOR", false, deviceId),
                    notifier.SendNotificationAsync("DESCRIPTION_SENSOR", state.NowPlayingDescription, deviceId),
                ]
                : [
                    notifier.SendNotificationAsync("PLAYING_SENSOR", state.PlayState is PlayState.Playing, deviceId),
                    notifier.SendNotificationAsync("TITLE_SENSOR", state.NowPlayingLabel, deviceId),
                    notifier.SendNotificationAsync("DESCRIPTION_SENSOR", state.NowPlayingDescription, deviceId),
                    this.NotifyCoverArtAsync(client, state.NowPlayingImage)
                ];
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Kodi player state notification for {DeviceId} failed.", client.DeviceId);
        }
    }

    private void Client_VolumeChanged(object? sender, DataEventArgs<int> e) => _ = this.HandleVolumeChangedAsync(sender, e.Data);

    internal async Task HandleVolumeChangedAsync(object? sender, int volume)
    {
        if (sender is not KodiClient { DeviceId: { } deviceId } || this.Notifier is not { } notifier)
        {
            return;
        }
        try
        {
            await notifier.SendNotificationAsync("VOLUME_SENSOR", volume, deviceId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Kodi volume notification for {DeviceId} failed.", deviceId);
        }
    }

    private void ClientManager_ClientAdded(object? sender, DataEventArgs<KodiClient> e) => _ = this.HandleClientAddedAsync(e.Data);

    internal async Task HandleClientAddedAsync(KodiClient client)
    {
        if (!this.HasDeviceId(client.DeviceId))
        {
            return;
        }
        try
        {
            await this.OnClientConnectedAsync(client).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Kodi connection handling for {DeviceId} failed.", client.DeviceId);
        }
    }

    private DiscoveredDevice CreateDiscoveredDevice(KodiClient client) => new(client.DeviceId, this.GetDisplayName(client), client.IsConnected);

    private void DetachEventHandlers(KodiClient client)
    {
        client.Connected -= this.Client_Connected;
        client.Disconnected -= this.Client_Disconnected;
        client.Error -= this.Client_Error;
        client.PlayerStateChanged -= this.Client_PlayerStateChanged;
        client.VolumeChanged -= this.Client_VolumeChanged;
    }

    private async Task<DiscoveredDevice[]> DiscoverAsync(string? deviceId, CancellationToken cancellationToken)
    {
        if (deviceId != null && this._clientManager.GetClientOrDefault(deviceId) is { } client)
        {
            return [this.CreateDiscoveredDevice(client)];
        }
        await this._clientManager.DiscoverAsync(3000, client => client.DeviceId == deviceId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(deviceId))
        {
            return [.. this._clientManager.Clients.Select(this.CreateDiscoveredDevice)];
        }
        if (this.GetClientOrDefault(deviceId) is { } deviceClient)
        {
            return [this.CreateDiscoveredDevice(deviceClient)];
        }
        return [];
    }

    private async Task HandleButtonAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        if (buttonName == TextAttribute.GetText(Buttons.PowerOn) && PhysicalAddress.TryParse(deviceId, out PhysicalAddress? macAddress))
        {
            await WakeOnLan.WakeAsync(macAddress, cancellationToken).ConfigureAwait(false);
        }
        if (this.GetClientOrDefault(deviceId) is not { } client)
        {
            this._logger.LogInformation("Can not process {button} as device is not ready.", buttonName);
            return;
        }
        Func<KodiClient, CancellationToken, Task>? function = Button.TryResolve(buttonName) is { } button
            ? KodiDeviceProviderBase._buttonFunctions.GetValueOrDefault(button)
            : KodiDeviceProviderBase._customButtonFunctions.GetValueOrDefault(buttonName);
        if (function == null)
        {
            return;
        }
        if (KodiDeviceProviderBase.IsClientReady(client))
        {
            await function(client, cancellationToken).ConfigureAwait(false);
            return;
        }
        // Not connected - a reconnect was already kicked off by IsClientReady. Queue this press to replay
        // once the connection comes back, rather than dropping it or blocking this call on the reconnect.
        // Only the most recently queued press survives, so a burst of presses while disconnected doesn't
        // replay all of them.
        this._logger.LogInformation("Device not connected, queuing {button} to replay once reconnected.", buttonName);
        client.QueuePendingButtonAction(function);
    }

    private bool HasDeviceId(string deviceId) => this._deviceIds.ContainsKey(deviceId);

    private async Task InitializeDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Initialized with [{deviceIds}]", string.Join(',', deviceIds));
        if (deviceIds.Length == 0)
        {
            return;
        }
        await this._clientManager.InitializeAsync(deviceId: deviceIds is [{ } id] ? id : null, cancellationToken).ConfigureAwait(false);
        foreach (string deviceId in deviceIds)
        {
            await this.OnDeviceAddedAsync(deviceId, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool IsClientReady(string deviceId) => this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client);

    private Task<bool> NotifyConnectedAsync(KodiClient client, CancellationToken cancellationToken = default)
    {
        return client.ShowNotificationAsync("NEEO", $"Connected to {this._deviceName}", Images.Neeo, cancellationToken: cancellationToken);
    }

    private Task<bool> NotifyDisconnectedAsync(KodiClient client, CancellationToken cancellationToken)
    {
        return client.ShowNotificationAsync("NEEO", $"Disconnected from {this._deviceName}", Images.Neeo, cancellationToken: cancellationToken);
    }

    private async Task OnClientConnectedAsync(KodiClient client)
    {
        this.AttachEventHandlers(client);
        if (KodiDeviceProviderBase.IsClientReady(client))
        {
            await this.HandleClientConnectedAsync(client).ConfigureAwait(false);
        }
    }

    private async Task OnDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this.AddDeviceId(deviceId);
        if (this.GetClientOrDefault(deviceId) is { } client)
        {
            await this.OnClientConnectedAsync(client).ConfigureAwait(false);
        }
    }

    private async Task OnDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this.RemoveDeviceId(deviceId);
        if (this.GetClientOrDefault(deviceId) is not { } client)
        {
            return;
        }
        this.DetachEventHandlers(client);
        if (client.IsConnected)
        {
            await this.NotifyDisconnectedAsync(client, cancellationToken).ConfigureAwait(false);
        }
    }

    private void RemoveDeviceId(string deviceId) => this._deviceIds.TryRemove(deviceId, out _);

    private void SetUriPrefix(string prefix)
    {
        this._uriPrefix = prefix;
        _ = RefreshCoverArtAsync();

        Task RefreshCoverArtAsync()
        {
            if (this._clientManager.Clients is not { Count: > 0 } clients)
            {
                return Task.CompletedTask;
            }
            List<Task> tasks = new(clients.Count);
            foreach (KodiClient client in clients)
            {
                if (this.HasDeviceId(client.DeviceId) && KodiDeviceProviderBase.IsClientReady(client))
                {
                    tasks.Add(NotifyAsync(client));
                }
            }
            return Task.WhenAll(tasks);

            async Task NotifyAsync(KodiClient client)
            {
                try
                {
                    await this.NotifyCoverArtAsync(client, client.PlayerState.NowPlayingImage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Failed to send cover art for {deviceId}.", client.DeviceId);
                }
            }
        }
    }

    private async Task NotifyCoverArtAsync(KodiClient client, string? image)
    {
        if (this.Notifier is { } notifier && this._uriPrefix.Length > 0)
        {
            await notifier.SendNotificationAsync("COVER_ART_SENSOR", this.GetThumbnailUrl(client, image, ImageSize.Large), client.DeviceId).ConfigureAwait(false);
        }
    }

    private readonly struct EmbeddedImages(string uriPrefix)
    {
        public string Filter => this.GetEmbeddedResourceUrl("filter.jpg");

        public string Movie => this.GetEmbeddedResourceUrl("movie.jpg");

        public string Music => this.GetEmbeddedResourceUrl("music.jpg");

        public string Pvr => this.GetEmbeddedResourceUrl("pvr.jpg");

        public string TVShow => this.GetEmbeddedResourceUrl("tvshow.jpg");

        private string GetEmbeddedResourceUrl(string suffix) => uriPrefix + suffix;
    }

    protected static class Constants
    {
        public const string DiscoveryDescription = "Ensure that 'Announce services to other systems' and HTTP control are enabled in Kodi (Settings -> Services -> Control).";
        public const string DiscoveryHeader = "Discovering Kodi";
        public const string HttpServiceName = "_xbmc-jsonrpc-h._tcp.local.";
        public const string RegistrationDescription = "Enter the username and password configured for Kodi web server (Settings -> Services -> Control).";
        public const string RegistrationHeader = "Kodi Authentication";
    }

    private readonly record struct Identifier(string Key, int Value, string Suffix);
}
