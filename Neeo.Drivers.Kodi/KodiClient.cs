using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.Kodi.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

public sealed class KodiClient(string displayName, IPAddress ipAddress, int httpPort, ILogger logger) : IDeviceClient, IDisposable
{
    private static readonly string[] _currentWindowProperties = ["currentwindow"];
    private static readonly string[] _getItemProperties = ["title", "showtitle", "season", "episode", "art", "thumbnail", "year", "genre", "album", "artist", "track"];

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _taskSources = new();
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Func<KodiClient, CancellationToken, Task>? _pendingButtonAction;
    private ReconnectLoop? _reconnectLoop;
    private ClientWebSocket? _webSocket;

    public event EventHandler? Connected;

    public event EventHandler? ConnectionChanged;

    public event EventHandler? Disconnected;

    public event EventHandler<DataEventArgs<string>>? Error;

    public event EventHandler<DataEventArgs<bool>>? IsMutedChanged;

    public event EventHandler<DataEventArgs<PlayerState>>? PlayerStateChanged;

    public event EventHandler<DataEventArgs<int>>? VolumeChanged;

    public string DeviceId => this.MacAddress.Equals(PhysicalAddress.None) ? this.IPAddress.ToString() : this.MacAddress.ToString();

    public string DisplayName => displayName;

    public IPAddress IPAddress => ipAddress;

    public int HttpPort => httpPort;

    private string? _userName;
    private string? _password;

    public void SetCredentials(string userName, string password)
    {
        this._userName = userName;
        this._password = password;
    }

    public bool IsConnected => this._webSocket is { State: WebSocketState.Open };

    public bool IsDisposed { get; private set; }

    public bool IsMuted
    {
        get;
        private set => this.SetValue(ref field, value, this.IsMutedChanged);
    }

    public PhysicalAddress MacAddress { get; private set; } = PhysicalAddress.None;

    public PlayerState PlayerState
    {
        get;
        private set => this.SetValue(ref field, value, this.PlayerStateChanged);
    } = PlayerState.Defaults;

    public int Volume
    {
        get;
        private set => this.SetValue(ref field, value, this.VolumeChanged);
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (this.IsConnected)
        {
            return true;
        }
        await this._connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.IsConnected)
            {
                return true;
            }
            return await this.PerformConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this._connectLock.Release();
        }
    }

    private async Task<bool> PerformConnectAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting to {ipAddress}:9090/jsonrpc...", this.IPAddress);
        ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = TimeSpan.FromSeconds(30d) } };
        CancellationTokenSource cts = new();
        try
        {
            await webSocket.ConnectAsync(new($"ws://{this.IPAddress}:9090/jsonrpc"), cancellationToken).ConfigureAwait(false);
            this._cancellationTokenSource = cts;
            this._webSocket = webSocket;
            _ = Task.Run(this.MessageLoop, cts.Token);
            await this.OnConnectedAsync().ConfigureAwait(false);
            logger.LogInformation("Connected to {ipAddress}:9090/jsonrpc...", this.IPAddress);
            if (Interlocked.Exchange(ref this._pendingButtonAction, null) is { } pendingAction)
            {
                _ = this.ReplayPendingActionAsync(pendingAction);
            }
            return true;
        }
        catch (WebSocketException)
        {
            logger.LogWarning("Failed to connect to {ipAddress}:9090/jsonrpc.", this.IPAddress);
            cts.Dispose();
            webSocket.Dispose();
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error connecting to {ipAddress}:9090/jsonrpc.", this.IPAddress);
            cts.Dispose();
            webSocket.Dispose();
            return false;
        }
    }

    public void Dispose()
    {
        if (this.IsDisposed)
        {
            return;
        }
        this.IsDisposed = true;
        this._reconnectLoop?.Dispose();
        this.CleanUp();
        this._connectLock.Dispose();
        this._sendLock.Dispose();
    }

    /// <summary>
    /// Queues an action to be replayed once the client (re)connects, replacing any previously queued
    /// action - only the most recently queued action survives, so a burst of button presses while
    /// disconnected doesn't replay all of them once the connection comes back.
    /// </summary>
    public void QueuePendingButtonAction(Func<KodiClient, CancellationToken, Task> action) => Interlocked.Exchange(ref this._pendingButtonAction, action);

    /// <summary>
    /// Requests a reconnection attempt as soon as possible (coalesced with any attempt already in flight). A no-op once disposed.
    /// </summary>
    public void RequestReconnect()
    {
        if (!this.IsDisposed)
        {
            this.GetReconnectLoop().TriggerNow();
        }
    }

    private async Task ReplayPendingActionAsync(Func<KodiClient, CancellationToken, Task> action)
    {
        try
        {
            await action(this, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to replay queued button action.");
        }
    }

    public Task<QueryData<AlbumInfo>> GetAlbumsAsync(int start = 0, int end = -1, int? artistId = default, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "AudioLibrary.GetAlbums",
        new { Sort = new SortOrder("title"), Limits = new { start, end }, Properties = AlbumInfo.Fields, Filter = artistId is int id ? new { artistid = id } : null },
        static (AlbumListResult result) => QueryData.Create(result.Limits.Total, result.Albums ?? []),
        cancellationToken
    );

    public Task<QueryData<ArtistInfo>> GetArtistsAsync(int start = 0, int end = -1, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "AudioLibrary.GetArtists",
        new { Sort = new SortOrder("artist"), Limits = new { start, end }, Properties = ArtistInfo.Fields },
        static (ArtistListResult result) => QueryData.Create(result.Limits.Total, result.Artists ?? []),
        cancellationToken
    );

    public Task<bool> GetBooleanAsync(string boolean, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "Xbmc.GetInfoBooleans",
        new { booleans = new[] { boolean } },
        (JsonElement element) => element.GetProperty(boolean).GetBoolean(),
        cancellationToken
    );

    public Task<QueryData<EpisodeInfo>> GetEpisodesAsync(int start = 0, int end = -1, int? tvShowId = default, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "VideoLibrary.GetEpisodes",
        new { Sort = new SortOrder("season"), Limits = new { start, end }, Properties = EpisodeInfo.Fields, tvshowid = tvShowId },
        static (EpisodeListResult result) => QueryData.Create(result.Limits.Total, result.Episodes ?? []),
        cancellationToken
    );

    public string GetImageUrl(string image)
    {
        if (string.IsNullOrEmpty(image))
        {
            return Images.Kodi;
        }
        if (this._userName is { Length: > 0 } user && this._password is { Length: > 0 } pass)
        {
            return $"http://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(pass)}@{this.IPAddress}:{httpPort}/vfs/{Uri.EscapeDataString(image)}";
        }
        return $"http://{this.IPAddress}:{httpPort}/vfs/{Uri.EscapeDataString(image)}";
    }

    public string GetRawImageUrl(string image) => $"http://{this.IPAddress}:{httpPort}/vfs/{Uri.EscapeDataString(image)}";

    public Task<string> GetLabelAsync(string label, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "Xbmc.GetInfoLabels",
        new { labels = new[] { label } },
        (JsonElement element) => element.GetProperty(label).GetString()!,
        cancellationToken
    );

    public Task<QueryData<VideoInfo>> GetMoviesAsync(int start = 0, int end = -1, Filter? filter = default, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "VideoLibrary.GetMovies",
        new { Sort = new SortOrder("title"), Limits = new { start, end }, Properties = VideoInfo.Fields, Filter = filter },
        static (MoviesListResult result) => QueryData.Create(result.Limits.Total, result.Movies ?? []),
        cancellationToken
    );

    public Task<PlayerDescriptor[]> GetPlayersAsync(CancellationToken cancellationToken) => this.SendMessageAsync(
        "Player.GetPlayers",
        static (PlayerDescriptor[] players) => players,
        cancellationToken
    );

    public Task<QueryData<AlbumInfo>> GetRecentlyAddedAlbumsAsync(int start = 0, int end = -1, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "AudioLibrary.GetRecentlyAddedAlbums",
        new { Limits = new { start, end }, Properties = AlbumInfo.Fields },
        static (AlbumListResult result) => QueryData.Create(result.Limits.Total, result.Albums ?? []),
        cancellationToken
    );

    public Task<QueryData<EpisodeInfo>> GetRecentlyAddedEpisodesAsync(int start = 0, int end = -1, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "VideoLibrary.GetRecentlyAddedEpisodes",
        new { Limits = new { start, end }, Properties = EpisodeInfo.Fields },
        static (EpisodeListResult result) => QueryData.Create(result.Limits.Total, result.Episodes ?? []),
        cancellationToken
    );

    public Task<QueryData<VideoInfo>> GetRecentlyAddedMoviesAsync(int start = 0, int end = -1, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "VideoLibrary.GetRecentlyAddedMovies",
        new { Limits = new { start, end }, Properties = VideoInfo.Fields },
        static (MoviesListResult result) => QueryData.Create(result.Limits.Total, result.Movies ?? []),
        cancellationToken
    );

    public Task<QueryData<SongInfo>> GetSongsAsync(int start = 0, int end = -1, int? albumId = default, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "AudioLibrary.GetSongs",
        new { Sort = new SortOrder("track"), Limits = new { start, end }, Properties = SongInfo.Fields, Filter = albumId is int id ? new { albumid = id } : null },
        static (SongListResult result) => QueryData.Create(result.Limits.Total, result.Songs ?? []),
        cancellationToken
    );

    public Task<QueryData<TVShowInfo>> GetTVShowsAsync(int start = 0, int end = -1, Filter? filter = default, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "VideoLibrary.GetTVShows",
        new { Sort = new SortOrder("title"), Limits = new { start, end }, Properties = TVShowInfo.Fields, Filter = filter },
        static (TVShowsListResult result) => QueryData.Create(result.Limits.Total, result.TVShows ?? []),
        cancellationToken
    );

    public Task<bool> OpenFileAsync(string key, int id, CancellationToken cancellationToken) => this.SendMessageAsync(
        "Player.Open",
        new { Item = new Dictionary<string, int>(1) { { key, id } } },
        static (string result) => result == "OK",
        cancellationToken
    );

    public Task<bool> ActivateWindowAsync(string window, CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "GUI.ActivateWindow",
        new { window },
        static (string result) => result == "OK",
        cancellationToken
    );

    public Task<bool> GoHomeAsync(CancellationToken cancellationToken = default) => this.ActivateWindowAsync("home", cancellationToken);

    public Task<bool> ScanAudioLibraryAsync(CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "AudioLibrary.Scan",
        static (string result) => result == "OK",
        cancellationToken
    );

    public Task<bool> ScanVideoLibraryAsync(CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "VideoLibrary.Scan",
        static (string result) => result == "OK",
        cancellationToken
    );

    public Task<bool> SendGoToCommandAsync(bool next, CancellationToken cancellationToken) => this.SendMessageAsync(
        "Player.GoTo",
        new { playerid = 1, to = next ? "next" : "previous" },
        static (string result) => result == "OK",
        cancellationToken
    );

    public async Task<bool> SendInputCommandAsync(InputCommand command, CancellationToken cancellationToken = default)
    {
        if (InputCommandAttribute.GetAttribute(command) is not { } attribute)
        {
            return false;
        }
        bool sent = await this.SendMessageAsync(attribute.Method, attribute.Action == null ? null : new { attribute.Action }, (string result) => result == "OK", cancellationToken).ConfigureAwait(false);
        if (!sent || !IsCursorCommand(command) || await this.GetCurrentWindowIdAsync(cancellationToken).ConfigureAwait(false) != 12005 || await this.GetBooleanAsync("VideoPlayer.HasMenu", cancellationToken).ConfigureAwait(false))
        {
            return sent;
        }
        return await this.SendMessageAsync(
            "Player.Seek",
            command switch
            {
                InputCommand.Left => CursorParameters(big: false, forward: false),
                InputCommand.Right => CursorParameters(big: false, forward: true),
                InputCommand.Down => CursorParameters(big: true, forward: false),
                InputCommand.Up => CursorParameters(big: true, forward: true),
                _ => default,
            },
            static (SeekResult result) => result.Percentage >= 0d,
            cancellationToken
        ).ConfigureAwait(false);

        static object CursorParameters(bool big, bool forward) => new { playerid = 1, value = new { step = string.Concat(big ? "big" : "small", forward ? "forward" : "backward") } };

        static bool IsCursorCommand(InputCommand command) => command is InputCommand.Right or InputCommand.Left or InputCommand.Up or InputCommand.Down;
    }

    public async Task<int> SetVolumeAsync(int volume, CancellationToken cancellationToken = default) => this.Volume = volume is >= 0 and <= 100
        ? await this.SendMessageAsync("Application.SetVolume", new { volume }, (int volume) => volume, cancellationToken).ConfigureAwait(false)
        : throw new ArgumentOutOfRangeException(nameof(volume));

    public Task<bool> ShowNotificationAsync(
        string title,
        string message,
        string? image = default,
        int displayTime = 5000,
        CancellationToken cancellationToken = default
    ) => this.SendMessageAsync("GUI.ShowNotification", new Notification(title, message, image, displayTime), static (string result) => result == "OK", cancellationToken);

    private Task<TResult> SendMessageAsync<TPayload, TResult>(
        string method,
        object? parameters,
        Func<TPayload, TResult> transform,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (this._webSocket is not { State: WebSocketState.Open } webSocket)
        {
            _ = this.ConnectAsync(cancellationToken);
            return Task.FromCanceled<TResult>(new(true));
        }

        return SendRequestAsync();

        async Task<TResult> SendRequestAsync()
        {
            JsonRpcRequest request = new(method, parameters);
            TaskCompletionSource<JsonElement> elementSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            this._taskSources.TryAdd(request.Id, elementSource);
            try
            {
                byte[] requestBytes = JsonSerializer.SerializeToUtf8Bytes(request, KodiJsonSerializerOptions.Default);
                await this._sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await webSocket.SendAsync(requestBytes.AsMemory(), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    this._sendLock.Release();
                }

                using CancellationTokenSource timeoutCts = new(TimeSpan.FromMilliseconds(2500d));
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                using (linkedCts.Token.Register(static state => ((TaskCompletionSource<JsonElement>)state!).TrySetCanceled(), elementSource))
                {
                    JsonElement element = await elementSource.Task.ConfigureAwait(false);
                    return transform(ExtractPayload(element));
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Kodi RPC request {Method} (id: {Id}) timed out.", method, request.Id);
                return await Task.FromCanceled<TResult>(new(true)).ConfigureAwait(false);
            }
            finally
            {
                this._taskSources.TryRemove(request.Id, out _);
            }

            TPayload ExtractPayload(JsonElement element)
            {
                if (typeof(TPayload) == typeof(JsonElement))
                {
                    return Unsafe.As<JsonElement, TPayload>(ref element);
                }
                try
                {
                    return element.Deserialize<TPayload>(KodiJsonSerializerOptions.Default)!;
                }
                catch (JsonException)
                {
                    logger.LogError("Failed to deserialize to {payload} from {element}.", typeof(TPayload), element);
                    throw;
                }
            }
        }
    }

    private void CancelOutstanding()
    {
        if (this._taskSources.IsEmpty)
        {
            return;
        }
        TaskCompletionSource<JsonElement>[] array = [.. this._taskSources.Values];
        this._taskSources.Clear();
        Array.ForEach(array, static source => source.TrySetCanceled());
    }

    private void CleanUp()
    {
        using WebSocket? webSocket = Interlocked.Exchange(ref this._webSocket, default);
        using CancellationTokenSource? source = Interlocked.Exchange(ref this._cancellationTokenSource, default);
        source?.Cancel();
    }

    private Task<int> GetCurrentWindowIdAsync(CancellationToken cancellationToken) => this.SendMessageAsync(
        "GUI.GetProperties",
        new { Properties = KodiClient._currentWindowProperties },
        static (JsonElement element) => element.GetProperty("currentwindow").GetProperty("id").GetInt32(),
        cancellationToken
    );

    private async Task<PhysicalAddress> GetMacAddressAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (await this.GetLabelAsync("Network.MacAddress").ConfigureAwait(false) is { } label && PhysicalAddress.TryParse(label, out PhysicalAddress? macAddress))
                {
                    return macAddress;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to query MAC address from Kodi (authentication may be required).");
                break;
            }
            await Task.Delay(200).ConfigureAwait(false);
        }
        return PhysicalAddress.None;
    }

    private Task<VolumeInfo> GetVolumeAsync(CancellationToken cancellationToken = default) => this.SendMessageAsync(
        "Application.GetProperties",
        new { VolumeInfo.Properties },
        (VolumeInfo info) => info,
        cancellationToken
    );

    private async Task MessageLoop()
    {
        try
        {
            if (this._cancellationTokenSource is { Token: { } token } && this._webSocket is { State: WebSocketState.Open } webSocket)
            {
                await JsonWebSocket.MessageLoop<JsonRpcResponse>(
                    webSocket,
                    (response, _) =>
                    {
                        if (response.Id is { } id && this._taskSources.TryRemove(id, out TaskCompletionSource<JsonElement>? taskSource))
                        {
                            if (response.Error is { Message: { } errorMessage })
                            {
                                taskSource.TrySetException(new ApplicationException($"Kodi RPC error: {errorMessage}"));
                                this.Error?.Invoke(this, errorMessage);
                            }
                            else if (response.Result is { } element)
                            {
                                taskSource.TrySetResult(element);
                            }
                            else
                            {
                                taskSource.TrySetResult(default);
                            }
                        }
                        else if (response.Error is { Message: { } errorMessage })
                        {
                            this.Error?.Invoke(this, errorMessage);
                        }
                        else if (response is { Method: { } method, Parameters.Data: { } responseData })
                        {
                            this.ProcessIncomingMessage(method, responseData);
                        }
                        return ValueTask.CompletedTask;
                    },
                    this.OnDisconnected,
                    options: KodiJsonSerializerOptions.Default,
                    cancellationToken: token
                ).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Kodi message loop terminated unexpectedly.");
        }
        finally
        {
            this.CancelOutstanding();
        }
    }

    private async Task OnConnectedAsync()
    {
        if (this.MacAddress.Equals(PhysicalAddress.None))
        {
            this.MacAddress = await this.GetMacAddressAsync().ConfigureAwait(false);
        }
        this.ConnectionChanged?.Invoke(this, EventArgs.Empty);
        this.Connected?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            this.ProcessVolumeInfo(await this.GetVolumeAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to query volume from Kodi.");
        }
        await this.QueryPlayerStateAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task QueryPlayerStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            PlayerInfo[] players = await this.SendMessageAsync(
                "Player.GetActivePlayers",
                static (PlayerInfo[] players) => players,
                cancellationToken
            ).ConfigureAwait(false);
            if (players is not [{ Id: { } activePlayerId }, ..])
            {
                this.PlayerState = PlayerState.Defaults;
                return;
            }
            JsonElement element = await this.SendMessageAsync(
                "Player.GetItem",
                new
                {
                    playerid = activePlayerId,
                    properties = KodiClient._getItemProperties,
                },
                static (JsonElement el) => el.GetProperty("item"),
                cancellationToken
            ).ConfigureAwait(false);
            string type = element.TryGetProperty("type", out JsonElement typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;
            this.PlayerState = type switch
            {
                "picture" when element.Deserialize<PictureInfo>(KodiJsonSerializerOptions.Default) is { } picture => new(
                    PlayState.Playing,
                    picture.Label,
                    string.Empty,
                    this.GetImageUrl(picture.Thumbnail)
                ),
                "episode" when element.Deserialize<EpisodeInfo>(KodiJsonSerializerOptions.Default) is { } episode => this.CreatePlayerState(episode),
                "song" when element.Deserialize<SongInfo>(KodiJsonSerializerOptions.Default) is { } song => this.CreatePlayerState(song),
                _ when element.Deserialize<VideoInfo>(KodiJsonSerializerOptions.Default) is { } video => this.CreatePlayerState(video),
                _ => PlayerState.Defaults,
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to query active player state on connect to Kodi.");
            this.PlayerState = PlayerState.Defaults;
        }
    }

    private void OnDisconnected()
    {
        this.CleanUp();
        this.PlayerState = PlayerState.Disconnected;
        this.ConnectionChanged?.Invoke(this, EventArgs.Empty);
        this.Disconnected?.Invoke(this, EventArgs.Empty);
        if (this.IsDisposed)
        {
            return;
        }
        this.GetReconnectLoop().Start();
    }

    // One loop for the client's lifetime, created on first use (a field initializer can't capture this).
    private ReconnectLoop GetReconnectLoop()
    {
        if (Volatile.Read(ref this._reconnectLoop) is { } existing)
        {
            return existing;
        }
        ReconnectLoop loop = new(TimeSpan.FromSeconds(15d), TimeSpan.FromMinutes(2d), this.ConnectAsync, logger);
        if (Interlocked.CompareExchange(ref this._reconnectLoop, loop, null) is { } winner)
        {
            loop.Dispose();
            return winner;
        }
        if (this.IsDisposed)
        {
            loop.Dispose();
        }
        return loop;
    }

    private async Task OnPlayAsync(PlayParameters parameters)
    {
        try
        {
            if (parameters.Item.Type is not ItemType.Episode and not ItemType.Movie and not ItemType.MusicVideo and not ItemType.Picture and not ItemType.Song)
            {
                return;
            }
            string[] properties = parameters.Item.Type switch
            {
                ItemType.Episode => EpisodeInfo.Fields,
                ItemType.Picture => PictureInfo.Fields,
                ItemType.Song => SongInfo.Fields,
                _ => VideoInfo.Fields, // Movie/MusicVideo
            };
            JsonElement element = await this.SendMessageAsync(
                "Player.GetItem",
                new { playerid = parameters.Player.Id, properties },
                static (JsonElement el) => el.GetProperty("item")
            ).ConfigureAwait(false);
            this.PlayerState = parameters.Item.Type switch
            {
                ItemType.Picture when element.Deserialize<PictureInfo>(KodiJsonSerializerOptions.Default) is { } picture => new(
                   PlayState.Playing,
                   picture.Label,
                   parameters.Item.File ?? parameters.Item.ToString(),
                   this.GetImageUrl(picture.Thumbnail)
                ),
                ItemType.Episode when element.Deserialize<EpisodeInfo>(KodiJsonSerializerOptions.Default) is { } episode => this.CreatePlayerState(episode),
                ItemType.Song when element.Deserialize<SongInfo>(KodiJsonSerializerOptions.Default) is { } song => this.CreatePlayerState(song),
                _ when element.Deserialize<VideoInfo>(KodiJsonSerializerOptions.Default) is { } video => this.CreatePlayerState(video),

                _ => this.PlayerState,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle Kodi Player.OnPlay notification.");
        }
    }

    private PlayerState CreatePlayerState(IMediaInfo media) => new(
        PlayState.Playing,
        media.GetTitle(),
        media.GetDescription(),
        this.GetImageUrl(media.GetCoverArt())
    );

    private void ProcessIncomingMessage(string method, JsonElement parameters)
    {
        switch (method)
        {
            case "Application.OnStop":
                this.PlayerState = PlayerState.Defaults;
                break;
            case "Application.OnVolumeChanged":
                if (parameters.Deserialize<VolumeInfo>(KodiJsonSerializerOptions.Default) is { } volumeInfo)
                {
                    this.ProcessVolumeInfo(volumeInfo);
                }
                break;
            case "Player.OnPause":
                this.PlayerState = this.PlayerState with { PlayState = PlayState.Paused };
                break;
            case "Player.OnPlay":
                if (parameters.Deserialize<PlayParameters>(KodiJsonSerializerOptions.Default) is { } playParams)
                {
                    _ = this.OnPlayAsync(playParams);
                }
                break;
        }
    }

    private void ProcessVolumeInfo(VolumeInfo info)
    {
        (int volume, bool muted) = info;
        this.Volume = volume;
        this.IsMuted = muted;
    }

    private Task<TResult> SendMessageAsync<TPayload, TResult>(
        string method,
        Func<TPayload, TResult> transform,
        CancellationToken cancellationToken = default
    ) => this.SendMessageAsync(method, default, transform, cancellationToken);

    private void SetValue<TValue>(ref TValue field, TValue value, EventHandler<DataEventArgs<TValue>>? valueChanged)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        valueChanged?.Invoke(this, value);
    }

    internal readonly record struct AlbumListResult(
        Limits Limits,
        AlbumInfo[]? Albums
    );

    internal readonly record struct ArtistListResult(
        Limits Limits,
        ArtistInfo[]? Artists
    );

    internal readonly record struct EpisodeListResult(
        Limits Limits,
        EpisodeInfo[]? Episodes
    );

    internal readonly record struct SongListResult(
        Limits Limits,
        SongInfo[]? Songs
    );

    internal readonly record struct ItemInfo(
        string? File,
        int Id,
        ItemType Type
    );

    internal readonly record struct JsonRpcError(
        int Code,
        string Message
    );

    internal readonly record struct JsonRpcRequest(
        string Method,
        [property: JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Parameters = null
    )
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        [JsonPropertyName("jsonrpc")]
        public string JsonRpcVersion { get; } = "2.0";
    }

    internal readonly record struct JsonRpcResponse(
        JsonRpcError? Error = default,
        string? Id = default,
        string? Method = default,
        [property: JsonPropertyName("params")] ResponseParameters? Parameters = default,
        JsonElement? Result = default
    );

    internal readonly record struct Limits(
        int Start,
        int End,
        int Total
    );

    internal readonly record struct MoviesListResult(
        Limits Limits,
        VideoInfo[] Movies
    );

    internal readonly record struct Notification(
        string Title,
        string Message,
        string? Image,
        [property: JsonPropertyName("displaytime")] int DisplayTime
    );

    internal readonly record struct PictureInfo(
        string Label,
        string Thumbnail
    )
    {
        public static readonly string[] Fields = ["thumbnail", "title"];
    }

    internal readonly record struct PlayerInfo(
        [property: JsonPropertyName("playerid")] int Id,
        int? Speed = default,
        PlayerType? Type = default
    );

    internal readonly record struct PlayParameters(
        ItemInfo Item,
        PlayerInfo Player
    );

    internal readonly record struct ResponseParameters(
        JsonElement Data,
        string Sender
    );

    internal readonly record struct SeekResult(
        double Percentage,
        Time Time,
        Time TotalTime
    );

    internal readonly record struct SortOrder(
        string Method,
        string Order = "ascending",
        [property: JsonPropertyName("ignorearticle")] bool IgnoreArticle = true
    );

    internal readonly record struct Time(
        int Hours,
        int Minutes,
        int Seconds,
        int Milliseconds
    );

    internal readonly record struct TVShowsListResult(
        Limits Limits,
        [property: JsonPropertyName("tvshows")] TVShowInfo[] TVShows
    );

    internal readonly record struct VolumeInfo(
        int Volume,
        bool Muted = false
    )
    {
        public static readonly string[] Properties = ["muted", "volume"];
    }
}
