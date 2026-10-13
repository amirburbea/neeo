using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        [Buttons.Back] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Back, cancellationToken),
        [Buttons.ChannelDown] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ChannelDown, cancellationToken),
        [Buttons.ChannelUp] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ChannelUp, cancellationToken),
        [Buttons.CursorDown] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Down, cancellationToken),
        [Buttons.CursorEnter] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Select, cancellationToken),
        [Buttons.CursorLeft] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Left, cancellationToken),
        [Buttons.CursorRight] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Right, cancellationToken),
        [Buttons.CursorUp] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Up, cancellationToken),
        [Buttons.Digit0] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number0, cancellationToken),
        [Buttons.Digit1] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number1, cancellationToken),
        [Buttons.Digit2] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number2, cancellationToken),
        [Buttons.Digit3] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number3, cancellationToken),
        [Buttons.Digit4] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number4, cancellationToken),
        [Buttons.Digit5] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number5, cancellationToken),
        [Buttons.Digit6] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number6, cancellationToken),
        [Buttons.Digit7] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number7, cancellationToken),
        [Buttons.Digit8] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number8, cancellationToken),
        [Buttons.Digit9] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Number9, cancellationToken),
        [Buttons.DigitSeparator] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.NumberSeperator, cancellationToken),
        [Buttons.Exit] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Back, cancellationToken),
        [Buttons.Home] = (client, cancellationToken) => client.GoHomeAsync(cancellationToken),
        [Buttons.Info] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Info, cancellationToken),
        [Buttons.Language] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Language, cancellationToken),
        [Buttons.Menu] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Menu, cancellationToken),
        [Buttons.MuteToggle] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.MuteToggle, cancellationToken),
        [Buttons.Next] = (client, cancellationToken) => client.SendGoToCommandAsync(next: true, cancellationToken),
        [Buttons.NextTrack] = (client, cancellationToken) => client.SendGoToCommandAsync(next: true, cancellationToken),
        [Buttons.Pause] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Pause, cancellationToken),
        [Buttons.Play] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Play, cancellationToken),
        [Buttons.PlayToggle] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Pause, cancellationToken),
        [Buttons.PlayPauseToggle] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Pause, cancellationToken),
        [Buttons.Previous] = (client, cancellationToken) => client.SendGoToCommandAsync(next: false, cancellationToken),
        [Buttons.PreviousTrack] = (client, cancellationToken) => client.SendGoToCommandAsync(next: false, cancellationToken),
        [Buttons.SkipBackward] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.SkipPrevious, cancellationToken),
        [Buttons.SkipForward] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.SkipNext, cancellationToken),
        [Buttons.Stop] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Stop, cancellationToken),
        [Buttons.Subtitle] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.Subtitle, cancellationToken),
        [Buttons.VolumeDown] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.VolumeDown, cancellationToken),
        [Buttons.VolumeUp] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.VolumeUp, cancellationToken),
    };

    private static readonly Dictionary<string, Func<KodiClient, CancellationToken, Task>> _customButtonFunctions = new(StringComparer.Ordinal)
    {
        ["CHANNEL SEARCH"] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ChannelSearch, cancellationToken),
        ["FAVORITE"] = (client, cancellationToken) => client.ActivateWindowAsync("pvrosdchannels", cancellationToken),
        ["OSD"] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.OnScreenDisplay, cancellationToken),
        ["PLAY PAUSE"] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.PlayPauseToggle, cancellationToken),
        ["TOGGLE FULLSCREEN"] = (client, cancellationToken) => client.SendInputCommandAsync(InputCommand.ToggleFullScreen, cancellationToken),
        ["Scan Video Library"] = (client, cancellationToken) => client.ScanVideoLibraryAsync(cancellationToken),
        ["Scan Audio Library"] = (client, cancellationToken) => client.ScanAudioLibraryAsync(cancellationToken),
    };

    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private static readonly TimeSpan _scanTime = TimeSpan.FromSeconds(5d);

    private readonly KodiClientManager _clientManager;
    private readonly Lazy<IDeviceBuilder> _deviceBuilder;
    private readonly HashSet<string> _deviceIds = [];
    private readonly string _deviceName;
    private readonly DeviceType _deviceType;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger _logger;
    private IDeviceNotifier? _notifier;
    private string _uriPrefix = string.Empty;

    protected KodiDeviceProviderBase(KodiClientManager clientManager, string deviceName, DeviceType deviceType, ILogger logger)
    {
        this._deviceBuilder = new(this.CreateDevice);
        this._clientManager = clientManager;
        this._deviceName = deviceName;
        this._deviceType = deviceType;
        this._logger = logger;
        this._clientManager.Registry.Changed += this.Registry_Changed;
    }

    public IDeviceBuilder DeviceBuilder => this._deviceBuilder.Value;

    public virtual void Dispose()
    {
        this._clientManager.Registry.Changed -= this.Registry_Changed;
        GC.SuppressFinalize(this);
    }

    protected static bool IsClientReady(KodiClient client)
    {
        if (client.IsConnected)
        {
            return true;
        }
        _ = client.ConnectAsync();
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
        .AddPowerStateSensor(this._clientManager.Registry, (deviceId, _) => this.GetIsPoweredOnAsync(deviceId))
        .EnableDeviceRoute(this.SetUriPrefix, this.HandleDeviceRouteAsync)
        .EnableDiscovery(Constants.DiscoveryHeader, Constants.DiscoveryDescription, this.DiscoverAsync)
        .EnableRegistration(Constants.RegistrationHeader, Constants.RegistrationDescription, this.QueryIsRegisteredAsync, this.RegisterAsync)
        .EnableNotifications(notifier => this._notifier = notifier)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, this.InitializeDeviceListAsync)
        .RegisterInitializer(cancellationToken => this._clientManager.InitializeAsync(cancellationToken: cancellationToken));

    protected KodiClient? GetClientOrDefault(string deviceId) => this._clientManager.GetClientOrDefault(deviceId);

    protected Task<TValue> GetClientValue<TValue>(
        string deviceId,
        Func<KodiClient, TValue> projection,
        TValue defaultValue
    ) => Task.FromResult(this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) ? projection(client) : defaultValue);

    protected Task<string> GetCoverArt(string deviceId) => this.GetClientValue(
        deviceId,
        client => this.GetThumbnailUrl(client, client.PlayerState.NowPlayingImage, ImageSize.Large),
        string.Empty
    );

    protected Task<bool> QueryIsRegisteredAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(this._clientManager.HasCredentials);

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

    protected Task<string> GetDescription(string deviceId) => this.GetClientValue(
        deviceId,
        client => client.PlayerState.NowPlayingDescription,
        string.Empty
    );

    protected virtual string GetDisplayName(KodiClient client) => client.DisplayName;

    protected Task<bool> GetIsMutedAsync(string deviceId) => this.GetClientValue(
        deviceId,
        client => client.IsMuted,
        default
    );

    protected Task<bool> GetIsPlaying(string deviceId) => this.GetClientValue(
        deviceId,
        client => client.PlayerState.PlayState == PlayState.Playing,
        default
    );

    protected Task<string> GetTitle(string deviceId) => this.GetClientValue(
        deviceId,
        client => client.PlayerState.NowPlayingLabel,
        string.Empty
    );

    protected Task<double> GetVolume(string deviceId) => this.GetClientValue(
        deviceId,
        client => (double)client.Volume,
        default
    );

    protected async Task HandleDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) && KodiDeviceProviderBase.TryTranslate(actionIdentifier) is { Key: { } key, Value: { } value })
        {
            await client.OpenFileAsync(key, value, cancellationToken).ConfigureAwait(false);
        }
    }

    protected async Task PopulateQueueDirectoryAsync(string deviceId, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is not { } client || !KodiDeviceProviderBase.IsClientReady(client))
        {
            builder.AddEntry(new("Kodi is not connected!", "Try reloading the list.", ThumbnailUri: Images.Kodi, UIAction: DirectoryUIAction.Reload));
            return;
        }
        builder.AddEntry(new("Queue is empty", "No items in current playlist.", ThumbnailUri: Images.Kodi));
        await Task.CompletedTask.ConfigureAwait(false);
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

    private string GetThumbnailUrl(KodiClient client, string? coverArt, ImageSize size = ImageSize.Large)
    {
        if (string.IsNullOrEmpty(coverArt) || coverArt == Images.Kodi)
        {
            return Images.Kodi;
        }
        if (string.IsNullOrEmpty(this._uriPrefix))
        {
            return coverArt.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || coverArt.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? coverArt
                : client.GetImageUrl(coverArt);
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

    private async Task<ActionResult> HandleDeviceRouteAsync(HttpRequest request, string path, CancellationToken cancellationToken) =>
        path.StartsWith("thumbnail/", StringComparison.OrdinalIgnoreCase)
            ? await this.HandleThumbnailRouteAsync(path[10..], cancellationToken).ConfigureAwait(false)
            : KodiDeviceProviderBase.HandleEmbeddedImageRoute(path);

    private static ActionResult HandleEmbeddedImageRoute(string path)
    {
        if (Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(KodiClient).Namespace}.Images.{path.Replace('/', '.')}") is not { } stream)
        {
            return new NotFoundResult();
        }
        string fileDownloadName = path.LastIndexOf('/') is int index and not -1 ? path[(index + 1)..] : path;
        return new FileStreamResult(stream, KodiDeviceProviderBase.GetContentType(fileDownloadName)) { FileDownloadName = fileDownloadName };
    }

    private async Task<ActionResult> HandleThumbnailRouteAsync(string suffix, CancellationToken cancellationToken)
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
            return new NotFoundResult();
        }

        return await this._clientManager.FetchImageAsync(imageUrl, maxDimension, cancellationToken).ConfigureAwait(false) is ({ } bytes, { } contentType)
            ? new FileContentResult(bytes, contentType)
            : new NotFoundResult();
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
        ? new(groups["key"].Value, int.Parse(groups["id"].Value), groups["suffix"].Value ?? string.Empty)
        : null;

    private void AddDeviceId(string deviceId)
    {
        try
        {
            this._lock.EnterWriteLock();
            this._deviceIds.Add(deviceId);
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
    }

    private void AttachEventHandlers(KodiClient client)
    {
        client.Connected += this.Client_Connected;
        client.Error += this.Client_Error;
        client.PlayerStateChanged += this.Client_PlayerStateChanged;
        client.VolumeChanged += this.Client_VolumeChanged;
    }

    // Power notifications on connect/disconnect are sent by the registry (see AddPowerStateSensor).
    private async void Client_Connected(object? sender, EventArgs e)
    {
        if (sender is not KodiClient client || !this.HasDeviceId(client.DeviceId))
        {
            return;
        }
        await client.RefreshStateAsync().ConfigureAwait(false);
        await this.NotifyConnectedAsync(client).ConfigureAwait(false);
    }

    private void Client_Error(object? sender, DataEventArgs<string> e) => this._logger.LogWarning("Client {deviceId} error encountered: {error}.", ((KodiClient)sender!).DeviceId, e.Data);

    private async void Client_PlayerStateChanged(object? sender, DataEventArgs<PlayerState> e)
    {
        if (sender is not KodiClient client || !this.HasDeviceId(client.DeviceId) || this._notifier is not { } notifier)
        {
            return;
        }
        string deviceId = client.DeviceId;
        List<Task> tasks = [];
        switch (e.Data.PlayState)
        {
            case PlayState.Paused:
                tasks.Add(notifier.SendNotificationAsync("PLAYING_SENSOR", false, deviceId));
                tasks.Add(notifier.SendNotificationAsync("DESCRIPTION_SENSOR", e.Data.NowPlayingDescription, deviceId));
                break;
            case PlayState.Stopped or PlayState.Playing:
                tasks.Add(notifier.SendNotificationAsync("PLAYING_SENSOR", e.Data.PlayState == PlayState.Playing, deviceId));
                tasks.Add(notifier.SendNotificationAsync("TITLE_SENSOR", e.Data.NowPlayingLabel, deviceId));
                tasks.Add(notifier.SendNotificationAsync("DESCRIPTION_SENSOR", e.Data.NowPlayingDescription, deviceId));
                tasks.Add(notifier.SendNotificationAsync("COVER_ART_SENSOR", this.GetThumbnailUrl(client, e.Data.NowPlayingImage, ImageSize.Large), deviceId));
                break;
        }
        if (tasks.Count != 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private async void Client_VolumeChanged(object? sender, DataEventArgs<int> e)
    {
        if (sender is KodiClient { DeviceId: { } deviceId } && this._notifier is { } notifier)
        {
            await notifier.SendNotificationAsync("VOLUME_SENSOR", e.Data, deviceId).ConfigureAwait(false);
        }
    }

    private async void Registry_Changed(object? sender, DeviceRegistryChangedEventArgs<KodiClient> e)
    {
        if (e.Kind == DeviceRegistryChangeKind.Removed)
        {
            this.DetachEventHandlers(e.Client);
        }
        else if (e.Kind == DeviceRegistryChangeKind.Added && this.HasDeviceId(e.DeviceId))
        {
            await this.OnClientConnectedAsync(e.Client).ConfigureAwait(false);
        }
    }

    private DiscoveredDevice CreateDiscoveredDevice(KodiClient client) => new(client.DeviceId, this.GetDisplayName(client), client.IsConnected);

    private void DetachEventHandlers(KodiClient client)
    {
        client.Connected -= this.Client_Connected;
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
        if ((client = this.GetClientOrDefault(deviceId)) != null)
        {
            return [this.CreateDiscoveredDevice(client)];
        }
        return [];
    }

    private Task<bool> GetIsPoweredOnAsync(string deviceId) => Task.FromResult(this.IsClientReady(deviceId));

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

    private bool HasDeviceId(string deviceId)
    {
        try
        {
            this._lock.EnterReadLock();
            return this._deviceIds.Contains(deviceId);
        }
        finally
        {
            this._lock.ExitReadLock();
        }
    }

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
            if (this.IsClientReady(deviceId) && this._notifier is { } notifier)
            {
                await notifier.SendPowerNotificationAsync(true, deviceId, cancellationToken).ConfigureAwait(false);
            }
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

    private async Task OnClientConnectedAsync(KodiClient client, CancellationToken cancellationToken = default)
    {
        this.AttachEventHandlers(client);
        if (KodiDeviceProviderBase.IsClientReady(client))
        {
            await client.RefreshStateAsync(cancellationToken).ConfigureAwait(false);
            await this.NotifyConnectedAsync(client, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this.AddDeviceId(deviceId);
        if (this.GetClientOrDefault(deviceId) is { } client)
        {
            await this.OnClientConnectedAsync(client, cancellationToken).ConfigureAwait(false);
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

    private void RemoveDeviceId(string deviceId)
    {
        try
        {
            this._lock.EnterWriteLock();
            this._deviceIds.Remove(deviceId);
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
    }

    private void SetUriPrefix(string prefix) => this._uriPrefix = prefix;

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
