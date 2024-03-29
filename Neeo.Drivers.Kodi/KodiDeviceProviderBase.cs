﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.Kodi.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

public abstract partial class KodiDeviceProviderBase : IDeviceProvider, IDisposable
{
    private static readonly Dictionary<Buttons, Func<KodiClient, Task>> _buttonFunctions = new()
    {
        [Buttons.Back] = client => client.SendInputCommandAsync(InputCommand.Back),
        [Buttons.CursorDown] = client => client.SendInputCommandAsync(InputCommand.Down),
        [Buttons.CursorEnter] = client => client.SendInputCommandAsync(InputCommand.Select),
        [Buttons.CursorLeft] = client => client.SendInputCommandAsync(InputCommand.Left),
        [Buttons.CursorRight] = client => client.SendInputCommandAsync(InputCommand.Right),
        [Buttons.CursorUp] = client => client.SendInputCommandAsync(InputCommand.Up),
        [Buttons.Digit0] = client => client.SendInputCommandAsync(InputCommand.Number0),
        [Buttons.Digit1] = client => client.SendInputCommandAsync(InputCommand.Number1),
        [Buttons.Digit2] = client => client.SendInputCommandAsync(InputCommand.Number2),
        [Buttons.Digit3] = client => client.SendInputCommandAsync(InputCommand.Number3),
        [Buttons.Digit4] = client => client.SendInputCommandAsync(InputCommand.Number4),
        [Buttons.Digit5] = client => client.SendInputCommandAsync(InputCommand.Number5),
        [Buttons.Digit6] = client => client.SendInputCommandAsync(InputCommand.Number6),
        [Buttons.Digit7] = client => client.SendInputCommandAsync(InputCommand.Number7),
        [Buttons.Digit8] = client => client.SendInputCommandAsync(InputCommand.Number8),
        [Buttons.Digit9] = client => client.SendInputCommandAsync(InputCommand.Number9),
        [Buttons.Language] = client => client.SendInputCommandAsync(InputCommand.Language),
        [Buttons.Menu] = client => client.SendInputCommandAsync(InputCommand.Menu),
        [Buttons.MuteToggle] = client => client.SendInputCommandAsync(InputCommand.MuteToggle),
        [Buttons.NextTrack] = client => client.SendGoToCommandAsync(next: true),
        [Buttons.Pause] = client => client.SendInputCommandAsync(InputCommand.Pause),
        [Buttons.Play] = client => client.SendInputCommandAsync(InputCommand.Play),
        [Buttons.PlayToggle] = client => client.SendInputCommandAsync(InputCommand.Pause),
        [Buttons.PlayPauseToggle] = client => client.SendInputCommandAsync(InputCommand.Pause),
        [Buttons.PreviousTrack] = client => client.SendGoToCommandAsync(next: false),
        [Buttons.Stop] = client => client.SendInputCommandAsync(InputCommand.Stop),
        [Buttons.VolumeDown] = client => client.SendInputCommandAsync(InputCommand.VolumeDown),
        [Buttons.VolumeUp] = client => client.SendInputCommandAsync(InputCommand.VolumeUp),
    };

    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private static readonly TimeSpan _scanTime = TimeSpan.FromSeconds(5d);

    private readonly KodiClientManager _clientManager;
    private readonly Lazy<IDeviceBuilder> _deviceBuilder;
    private readonly HashSet<string> _deviceIds = new();
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
        this._clientManager.ClientDiscovered += this.ClientManager_ClientDiscovered;
    }

    public IDeviceBuilder DeviceBuilder => this._deviceBuilder.Value;

    public virtual void Dispose()
    {
        this._clientManager.Dispose();
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
        .AddPowerStateSensor(this.GetIsPoweredOnAsync)
        .EnableDeviceRoute(this.SetUriPrefix, static (_, path, _) => Task.FromResult(KodiDeviceProviderBase.HandleDeviceRoute(path)))
        .EnableDiscovery(Constants.DiscoveryHeader, Constants.DiscoveryDescription, this.DiscoverAsync)
        .EnableNotifications(notifier => this._notifier = notifier)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, this.InitializeDeviceListAsync)
        .RegisterInitializer(cancellationToken => this._clientManager.InitializeAsync(cancellationToken: cancellationToken));

    protected KodiClient? GetClientOrDefault(string deviceId) => this._clientManager.GetClientOrDefault(deviceId);

    protected Task<TValue> GetClientValueAsync<TValue>(
        string deviceId,
        Func<KodiClient, TValue> projection,
        TValue defaultValue
    ) => Task.FromResult(this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) ? projection(client) : defaultValue);

    protected Task<string> GetCoverArtAsync(string deviceId) => this.GetClientValueAsync(
        deviceId,
        client => client.PlayerState.NowPlayingImage,
        string.Empty
    );

    protected Task<string> GetDescriptionAsync(string deviceId) => this.GetClientValueAsync(
        deviceId,
        client => client.PlayerState.NowPlayingDescription,
        string.Empty
    );

    protected virtual string GetDisplayName(KodiClient client) => client.DisplayName;

    protected Task<bool> GetIsMutedAsync(string deviceId) => this.GetClientValueAsync(
        deviceId,
        client => client.IsMuted,
        default
    );

    protected Task<bool> GetIsPlayingAsync(string deviceId) => this.GetClientValueAsync(
        deviceId,
        client => client.PlayerState.PlayState == PlayState.Playing,
        default
    );

    protected Task<string> GetTitleAsync(string deviceId) => this.GetClientValueAsync(
        deviceId,
        client => client.PlayerState.NowPlayingLabel,
        string.Empty
    );

    protected Task<double> GetVolumeAsync(string deviceId) => this.GetClientValueAsync(
        deviceId,
        client => (double)client.Volume,
        default
    );

    protected async Task HandleDirectoryActionAsync(string deviceId, string actionIdentifier)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) && KodiDeviceProviderBase.TryTranslate(actionIdentifier) is { Key: { } key, Value: { } value })
        {
            await client.OpenFileAsync(key, value).ConfigureAwait(false);
        }
    }

    protected async Task PopulateQueueDirectoryAsync(string deviceId, ListBuilder builder)
    {
        if (this.GetClientOrDefault(deviceId) is not { } client || !KodiDeviceProviderBase.IsClientReady(client))
        {
            builder.AddEntry(new("Kodi is not connected!", "Try reloading the list.", thumbnailUri: Images.Kodi, uiAction: ListUIAction.Reload));
            return;
        }
        EmbeddedImages images = new(this._uriPrefix);
        await Task.CompletedTask.ConfigureAwait(false);
        throw new NotImplementedException();
    }

    protected async Task PopulateRootDirectoryAsync(string deviceId, ListBuilder builder)
    {
        if (this.GetClientOrDefault(deviceId) is not { } client || !KodiDeviceProviderBase.IsClientReady(client))
        {
            builder.AddEntry(new("Kodi is not connected!", "Click to attempt reloading the list", thumbnailUri: Images.Kodi, uiAction: ListUIAction.Reload));
            return;
        }
        EmbeddedImages images = new(this._uriPrefix);
        string identifier = builder.Parameters.BrowseIdentifier ?? String.Empty;
        int offset = builder.Parameters.Offset ?? 0;
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
                await KodiDeviceProviderBase.PopulateMoviesLibraryAsync(client, builder, offset, limit).ConfigureAwait(false);
                break;
            case ".movies.inprogress":
                await KodiDeviceProviderBase.PopulateMoviesLibraryAsync(client, builder, offset, limit, new("inprogress", "true")).ConfigureAwait(false);
                break;
            case ".movies.unwatched":
                await KodiDeviceProviderBase.PopulateMoviesLibraryAsync(client, builder, offset, limit, new("playcount", "0")).ConfigureAwait(false);
                break;
            case ".movies.watched":
                await KodiDeviceProviderBase.PopulateMoviesLibraryAsync(client, builder, offset, limit, new("playcount", "0", FilterOperator.GreaterThan)).ConfigureAwait(false);
                break;
            case ".movies.recent":
                break;
            case ".music":
                KodiDeviceProviderBase.PopulateMusicLibraryRoot(builder, images);
                break;
            case ".music.albums":
            case ".music.albums.recent":
            case ".music.artists":
                break;
            case ".tvshows":
                KodiDeviceProviderBase.PopulateTVShowsLibraryRoot(builder, images);
                break;
            case ".tvshows.tvshows":
                await KodiDeviceProviderBase.PopulateTVShowsLibraryAsync(client, builder, offset, limit).ConfigureAwait(false);
                break;
            case ".tvshows.recent":
                break;
            case ".pvr":
                KodiDeviceProviderBase.PopulatePvrLibraryRoot(builder, images);
                break;
            case ".pvr.tvchannels":
            case ".pvr.radiostations":
                break;
            default:
                if (KodiDeviceProviderBase.TryTranslate(identifier) is { Key: "tvshowid", Value: int value, Suffix: { } suffix })
                {
                    await KodiDeviceProviderBase.PopulateEpisodesLibraryAsync(client, builder, offset, limit, value, suffix).ConfigureAwait(false);
                }
                break;
        }
    }

    protected async Task SetVolumeAsync(string deviceId, double value)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client))
        {
            await client.SetVolumeAsync((int)value).ConfigureAwait(false);
        }
    }

    private static ListEntry CreateListEntry(KodiClient client, IMediaInfo media, bool isAction = true, string? suffix = default)
    {
        string id = suffix == null ? media.GetId() : $"{media.GetId()}:{suffix}";
        return new(
            media.GetTitle(),
            media.GetDescription(),
            browseIdentifier: isAction ? null : id,
            actionIdentifier: isAction ? id : null,
            thumbnailUri: client.GetImageUrl(media.GetCoverArt())
        );
    }

    private static string GetContentType(string fileName) => KodiDeviceProviderBase._contentTypeProvider.TryGetContentType(fileName, out string? contentType)
        ? contentType
        : "application/octet-stream";

    private static ActionResult HandleDeviceRoute(string path)
    {
        if (Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(KodiClient).Namespace}.Images.{path.Replace('/', '.')}") is not { } stream)
        {
            return new NotFoundResult();
        }
        string fileDownloadName = path.LastIndexOf('/') is int index and not -1 ? path[(index + 1)..] : path;
        return new FileStreamResult(stream, KodiDeviceProviderBase.GetContentType(fileDownloadName)) { FileDownloadName = fileDownloadName };
    }

    [GeneratedRegex("^(?<key>[a-z]+)[:](?<id>[\\d]+)([:](?<suffix>.+))?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex IdentifierRegex();

    private static async Task PopulateAlbumLibraryAsync(KodiClient client, ListBuilder builder, int offset, int limit)
    {
        int end = offset + limit;
        (int total, AlbumInfo[] albums) = await client.GetAlbumsAsync(offset, end).ConfigureAwait(false);
    }

    private static async Task PopulateEpisodesLibraryAsync(KodiClient client, ListBuilder builder, int offset, int limit, int? tvShowId = default, string? tvShowLabel = default)
    {
        int end = offset + limit;
        (int total, EpisodeInfo[] episodes) = await client.GetEpisodesAsync(offset, end, tvShowId).ConfigureAwait(false);
        if (offset == 0 && builder.BrowseIdentifier is { } id)
        {
            if (!string.IsNullOrEmpty(tvShowLabel))
            {
                builder.AddHeader(tvShowLabel);
            }
            builder.AddButtonRow(new("Library", ".root", inverse: true, uiAction: ListUIAction.GoToRoot), new("Close", ".close", inverse: true, uiAction: ListUIAction.Close));
        }
        foreach (EpisodeInfo episode in episodes)
        {
            builder.AddEntry(KodiDeviceProviderBase.CreateListEntry(client, episode));
        }
        builder.SetTotalMatchingItems(total);
    }

    private static void PopulateLibraryRoot(ListBuilder list, EmbeddedImages images) => list
        .AddTileRow(new ListTile(Images.Kodi))
        .AddEntry(new("Movies", thumbnailUri: images.Movie, browseIdentifier: ".movies"))
        .AddEntry(new("Music", thumbnailUri: images.Music, browseIdentifier: ".music"))
        .AddEntry(new("TV Shows", thumbnailUri: images.TVShow, browseIdentifier: ".tvshows"))
        .AddEntry(new("PVR", thumbnailUri: images.Pvr, browseIdentifier: ".pvr"));

    private static async Task PopulateMoviesLibraryAsync(KodiClient client, ListBuilder builder, int offset, int limit, Filter? filter = default)
    {
        int end = offset + limit;
        (int total, VideoInfo[] videos) = await client.GetMoviesAsync(offset, end, filter).ConfigureAwait(false);
        if (offset == 0 && builder.BrowseIdentifier is { } id)
        {
            builder
                .AddHeader(id == ".movies" ? "Movies" : $"Movies ({id[8..]})")
                .AddButtonRow(new("Library", ".root", inverse: true, uiAction: ListUIAction.GoToRoot), new("Close", ".close", inverse: true, uiAction: ListUIAction.Close));
        }
        foreach (VideoInfo video in videos)
        {
            builder.AddEntry(KodiDeviceProviderBase.CreateListEntry(client, video));
        }
        builder.SetTotalMatchingItems(total);
    }

    private static void PopulateMoviesLibraryRoot(ListBuilder list, EmbeddedImages images) => list
        .AddHeader("Movies")
        .AddEntry(new("Movies", thumbnailUri: images.Movie, browseIdentifier: ".movies.movies"))
        .AddEntry(new("Movies - In Progress", thumbnailUri: images.Movie, browseIdentifier: ".movies.inprogress"))
        .AddEntry(new("Movies - Unwatched", thumbnailUri: images.Movie, browseIdentifier: ".movies.unwatched"))
        .AddEntry(new("Movies - Watched", thumbnailUri: images.Movie, browseIdentifier: ".movies.watched"))
        .AddEntry(new("Movies - Recent", thumbnailUri: images.Movie, browseIdentifier: ".movies.recent"));

    private static void PopulateMusicLibraryRoot(ListBuilder list, EmbeddedImages images) => list
        .AddHeader("Music")
        .AddEntry(new("Albums", thumbnailUri: images.Music, browseIdentifier: ".music.albums"))
        .AddEntry(new("Albums - Recent", thumbnailUri: images.Music, browseIdentifier: ".music.albums.recent"))
        .AddEntry(new("Artists", thumbnailUri: images.Music, browseIdentifier: ".music.artists"));

    private static void PopulatePvrLibraryRoot(ListBuilder list, EmbeddedImages images) => list
        .AddHeader("PVR")
        .AddEntry(new("TV Channels", thumbnailUri: images.Pvr, browseIdentifier: ".pvr.tvchannels"))
        .AddEntry(new("Radio Stations", thumbnailUri: images.Pvr, browseIdentifier: ".pvr.radiostations"));

    private static async Task PopulateTVShowsLibraryAsync(KodiClient client, ListBuilder builder, int offset, int limit, Filter? filter = default)
    {
        int end = offset + limit;
        (int total, TVShowInfo[] shows) = await client.GetTVShowsAsync(offset, end, filter).ConfigureAwait(false);
        if (offset == 0 && builder.BrowseIdentifier is { } id)
        {
            builder
                .AddHeader(id == ".tvshows" ? "TV Shows" : $"TV Shows ({id[9..]})")
                .AddButtonRow(new("Library", ".root", inverse: true, uiAction: ListUIAction.GoToRoot), new("Close", ".close", inverse: true, uiAction: ListUIAction.Close));
        }
        foreach (TVShowInfo show in shows)
        {
            builder.AddEntry(KodiDeviceProviderBase.CreateListEntry(client, show, isAction: false, suffix: show.GetDescription()));
        }
        builder.SetTotalMatchingItems(total);
    }

    private static void PopulateTVShowsLibraryRoot(ListBuilder list, EmbeddedImages images) => list
        .AddHeader("TV Shows")
        .AddEntry(new("TV Shows", thumbnailUri: images.TVShow, browseIdentifier: ".tvshows.tvshows"))
        .AddEntry(new("TV Shows - Recent", thumbnailUri: images.TVShow, browseIdentifier: ".tvshows.recent"));

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
        client.Disconnected += this.Client_Disconnected;
        client.Error += this.Client_Error;
        client.PlayerStateChanged += this.Client_PlayerStateChanged;
        client.VolumeChanged += this.Client_VolumeChanged;
    }

    private async void Client_Connected(object? sender, EventArgs e)
    {
        if (sender is not KodiClient client || this._notifier is not { } notifier)
        {
            return;
        }
        await Task.WhenAll(
            notifier.SendPowerNotificationAsync(true, client.DeviceId),
            this.NotifyConnectedAsync(client)
        ).ConfigureAwait(false);
    }

    private async void Client_Disconnected(object? sender, EventArgs e)
    {
        if (sender is KodiClient client && this._notifier is { } notifier)
        {
            await notifier.SendPowerNotificationAsync(false, client.DeviceId).ConfigureAwait(false);
        }
    }

    private void Client_Error(object? sender, DataEventArgs<string> e) => this._logger.LogWarning("Client {deviceId} error encountered: {error}.", ((KodiClient)sender!).DeviceId, e.Data);

    private async void Client_PlayerStateChanged(object? sender, DataEventArgs<PlayerState> e)
    {
        if (sender is not KodiClient { DeviceId: { } deviceId } || this._notifier is not { } notifier)
        {
            return;
        }
        List<Task> tasks = new();
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
                tasks.Add(notifier.SendNotificationAsync("COVER_ART_SENSOR", e.Data.NowPlayingImage, deviceId));
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

    private async void ClientManager_ClientDiscovered(object? sender, DataEventArgs<KodiClient> e)
    {
        KodiClient client = e.Data;
        if (this.HasDeviceId(client.DeviceId))
        {
            await this.OnClientConnectedAsync(client).ConfigureAwait(false);
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
            return new[] { this.CreateDiscoveredDevice(client) };
        }
        await this._clientManager.DiscoverAsync(1000, client => client.DeviceId == deviceId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(deviceId))
        {
            return this._clientManager.Clients.Select(this.CreateDiscoveredDevice).ToArray();
        }
        if ((client = this.GetClientOrDefault(deviceId)) != null)
        {
            return new[] { this.CreateDiscoveredDevice(client) };
        }
        return Array.Empty<DiscoveredDevice>();
    }

    private Task<bool> GetIsPoweredOnAsync(string deviceId) => Task.FromResult(this.IsClientReady(deviceId));

    private async Task HandleButtonAsync(string deviceId, string buttonName)
    {
        if (buttonName == TextAttribute.GetText(Buttons.PowerOn) && PhysicalAddress.TryParse(deviceId, out PhysicalAddress? macAddress))
        {
            await WakeOnLan.WakeAsync(macAddress).ConfigureAwait(false);
        }
        if (this.GetClientOrDefault(deviceId) is not { } client || !KodiDeviceProviderBase.IsClientReady(client))
        {
            this._logger.LogInformation("Can not process {button} as device is not ready.", buttonName);
            return;
        }
        if (Button.TryResolve(buttonName) is { } button && KodiDeviceProviderBase._buttonFunctions.GetValueOrDefault(button) is { } function)
        {
            await function(client).ConfigureAwait(false);
            return;
        }
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

    private async Task InitializeDeviceListAsync(string[] deviceIds)
    {

        await this._clientManager.InitializeAsync(deviceId: deviceIds is [{ } id] ? id : null).ConfigureAwait(false);
        if (deviceIds.Length == 0)
        {
            return;
        }
        foreach (string deviceId in deviceIds)
        {
            await this.OnDeviceAddedAsync(deviceId).ConfigureAwait(false);
            if (this.IsClientReady(deviceId) && this._notifier is { } notifier)
            {
                await notifier.SendPowerNotificationAsync(true, deviceId).ConfigureAwait(false);
            }
        }
    }

    private bool IsClientReady(string deviceId) => this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client);

    private Task NotifyConnectedAsync(KodiClient client) => client.ShowNotificationAsync("NEEO", $"Connected to {this._deviceName}", Images.Neeo);

    private Task NotifyDisconnectedAsync(KodiClient client) => client.ShowNotificationAsync("NEEO", $"Disconnected from {this._deviceName}", Images.Neeo);

    private async Task OnClientConnectedAsync(KodiClient client)
    {
        this.AttachEventHandlers(client);
        if (KodiDeviceProviderBase.IsClientReady(client))
        {
            await this.NotifyConnectedAsync(client).ConfigureAwait(false);
        }
    }

    private async Task OnDeviceAddedAsync(string deviceId)
    {
        this.AddDeviceId(deviceId);
        if (this.GetClientOrDefault(deviceId) is { } client)
        {
            await this.OnClientConnectedAsync(client).ConfigureAwait(false);
        }
    }

    private async Task OnDeviceRemovedAsync(string deviceId)
    {
        this.RemoveDeviceId(deviceId);
        if (this.GetClientOrDefault(deviceId) is not { } client)
        {
            return;
        }
        this.DetachEventHandlers(client);
        if (client.IsConnected)
        {
            await this.NotifyDisconnectedAsync(client).ConfigureAwait(false);
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

    private readonly struct EmbeddedImages
    {
        private readonly string _uriPrefix;

        public EmbeddedImages(string uriPrefix) => this._uriPrefix = uriPrefix;

        public string Filter => this.GetEmbeddedResourceUrl("filter.jpg");

        public string Movie => this.GetEmbeddedResourceUrl("movie.jpg");

        public string Music => this.GetEmbeddedResourceUrl("music.jpg");

        public string Pvr => this.GetEmbeddedResourceUrl("pvr.jpg");

        public string TVShow => this.GetEmbeddedResourceUrl("tvshow.jpg");

        private string GetEmbeddedResourceUrl(string suffix) => this._uriPrefix + suffix;
    }

    protected static class Constants
    {
        public const string DiscoveryDescription = "Ensure that 'Announce services to other systems' as well as HTTP control are enabled and that authentication is disabled.";
        public const string DiscoveryHeader = "Discovering Kodi";
        public const string HttpServiceName = "_xbmc-jsonrpc-h._tcp.local.";
    }

    private readonly record struct Identifier(string Key, int Value, string Suffix);
}