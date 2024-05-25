using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

public sealed class KodiClient(string displayName, IPAddress ipAddress, int httpPort, ILogger logger) : IDisposable
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _taskSources = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task<bool>? _connectTask;
    private bool _isMuted;
    private PlayerState _playerState = PlayerState.Defaults;
    private int _volume;
    private ClientWebSocket? _webSocket;

    public event EventHandler? Connected;

    public event EventHandler? Disconnected;

    public event EventHandler<DataEventArgs<string>>? Error;

    public event EventHandler<DataEventArgs<bool>>? IsMutedChanged;

    public event EventHandler<DataEventArgs<PlayerState>>? PlayerStateChanged;

    public event EventHandler<DataEventArgs<int>>? VolumeChanged;

    private event EventHandler? Disposed;

    public string DeviceId => this.MacAddress.ToString();

    public string DisplayName { get; } = displayName;

    public IPAddress IPAddress { get; } = ipAddress;

    public bool IsConnected => this._webSocket is { State: WebSocketState.Open };

    public bool IsDisposed { get; private set; }

    public bool IsMuted
    {
        get => this._isMuted;
        private set => this.SetValue(ref this._isMuted, value, this.IsMutedChanged);
    }

    public PhysicalAddress MacAddress { get; private set; } = PhysicalAddress.None;

    public PlayerState PlayerState
    {
        get => this._playerState;
        private set => this.SetValue(ref this._playerState, value, this.PlayerStateChanged);
    }

    public int Volume
    {
        get => this._volume;
        private set => this.SetValue(ref this._volume, value, this.VolumeChanged);
    }

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        return this._connectTask ??= ConnectAsync();

        async Task<bool> ConnectAsync()
        {
            logger.LogInformation("Connecting to {ipAddress}:9090/jsonrpc...", this.IPAddress);
            ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = TimeSpan.FromSeconds(30d) } };
            CancellationTokenSource cts = new();
            try
            {
                await webSocket.ConnectAsync(new($"ws://{this.IPAddress}:9090/jsonrpc"), cancellationToken).ConfigureAwait(false);
                this._cancellationTokenSource = cts;
                this._webSocket = webSocket;
                _ = Task.Factory.StartNew(this.MessageLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                await this.OnConnected().ConfigureAwait(false);
                logger.LogInformation("Connected to {ipAddress}:9090/jsonrpc...", this.IPAddress);
                return true;
            }
            catch (WebSocketException)
            {
                logger.LogWarning("Failed to connect to {ipAddress}:9090/jsonrpc.", this.IPAddress);
                cts.Dispose();
                webSocket.Dispose();
                return false;
            }
            finally
            {
                this._connectTask = null;
            }
        }
    }

    public void Dispose()
    {
        this.CleanUp();
        if (this.IsDisposed)
        {
            return;
        }
        this.IsDisposed = true;
        this.Disposed?.Invoke(this, EventArgs.Empty);
    }

    public Task<QueryData<AlbumInfo>> GetAlbumsAsync(int start = 0, int end = -1, int? artistId = default) => this.SendMessageAsync(
        "AudioLibrary.GetAlbums",
        new { Sort = new SortOrder("title"), Limits = new { start, end }, Properties = AlbumInfo.Fields },
        (AlbumListResult result) => QueryData.Create(result.Limits.Total, result.Albums)
    );

    public Task<bool> GetBooleanAsync(string boolean) => this.SendMessageAsync(
        "Xbmc.GetInfoBooleans",
        new { booleans = new[] { boolean } },
        (JsonElement element) => element.GetProperty(boolean).GetBoolean()
    );

    public Task<QueryData<EpisodeInfo>> GetEpisodesAsync(int start = 0, int end = -1, int? tvShowId = default) => this.SendMessageAsync(
        "VideoLibrary.GetEpisodes",
        new { Sort = new SortOrder("season"), Limits = new { start, end }, Properties = EpisodeInfo.Fields, tvshowid = tvShowId },
        (EpisodeListResult result) => QueryData.Create(result.Limits.Total, result.Episodes)
    );

    public string GetImageUrl(string image) => $"http://{this.IPAddress}:{httpPort}/vfs/{Uri.EscapeDataString(image)}";

    public Task<string> GetLabelAsync(string label) => this.SendMessageAsync(
        "Xbmc.GetInfoLabels",
        new { labels = new[] { label } },
        (JsonElement element) => element.GetProperty(label).GetString()!
    );

    public Task<QueryData<VideoInfo>> GetMoviesAsync(int start = 0, int end = -1, Filter? filter = default) => this.SendMessageAsync(
        "VideoLibrary.GetMovies",
        new { Sort = new SortOrder("title"), Limits = new { start, end }, Properties = VideoInfo.Fields, Filter = filter },
        (MoviesListResult result) => QueryData.Create(result.Limits.Total, result.Movies)
    );

    public Task<PlayerDescriptor[]> GetPlayersAsync() => this.SendMessageAsync("Player.GetPlayers", (PlayerDescriptor[] players) => players);

    public Task<QueryData<TVShowInfo>> GetTVShowsAsync(int start = 0, int end = -1, Filter? filter = default) => this.SendMessageAsync(
        "VideoLibrary.GetTVShows",
        new { Sort = new SortOrder("title"), Limits = new { start, end }, Properties = TVShowInfo.Fields, Filter = filter },
        (TVShowsListResult result) => QueryData.Create(result.Limits.Total, result.TVShows)
    );

    public Task<bool> OpenFileAsync(string key, int id) => this.SendMessageAsync(
        "Player.Open",
        new { Item = new Dictionary<string, int>(1) { { key, id } } },
        (string result) => result == "OK"
    );

    public Task<bool> SendGoToCommandAsync(bool next) => this.SendMessageAsync(
        "Player.GoTo",
        new { playerid = 1, to = next ? "next" : "previous" },
        (string result) => result == "OK"
    );

    public async Task<bool> SendInputCommandAsync(InputCommand command)
    {
        if (InputCommandAttribute.GetAttribute(command) is not { } attribute)
        {
            return false;
        }
        bool sent = await this.SendMessageAsync(attribute.Method, attribute.Action == null ? null : new { attribute.Action }, (string result) => result == "OK").ConfigureAwait(false);
        if (!sent || !IsCursorCommand(command) || await this.GetCurrentWindowIdAsync().ConfigureAwait(false) != 12005 || await this.GetBooleanAsync("VideoPlayer.HasMenu").ConfigureAwait(false))
        {
            return sent;
        }
        return await this.SendMessageAsync(
            command is InputCommand.Select ? "Input.ShowOSD" : "Player.Seek",
            command switch
            {
                InputCommand.Left => CursorParameters(big: false, forward: false),
                InputCommand.Right => CursorParameters(big: false, forward: true),
                InputCommand.Down => CursorParameters(big: true, forward: false),
                InputCommand.Up => CursorParameters(big: true, forward: true),
                _ => default,
            },
            (SeekResult result) => result.Percentage >= 0d
        ).ConfigureAwait(false);

        static object CursorParameters(bool big, bool forward) => new { playerid = 1, value = new { step = string.Concat(big ? "big" : "small", forward ? "forward" : "backward") } };

        static bool IsCursorCommand(InputCommand command) => command is InputCommand.Right or InputCommand.Left or InputCommand.Up or InputCommand.Down or InputCommand.Select;
    }

    public async Task<int> SetVolumeAsync(int volume) => this.Volume = volume is >= 0 and <= 100
        ? await this.SendMessageAsync("Application.SetVolume", new { volume }, (int volume) => volume).ConfigureAwait(false)
        : throw new ArgumentOutOfRangeException(nameof(volume));

    public Task<bool> ShowNotificationAsync(
        string title,
        string message,
        string? image = default,
        int displayTime = 5000
    ) => this.SendMessageAsync("GUI.ShowNotification", new Notification(title, message, image, displayTime), (string result) => result == "OK");

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

    private Task<int> GetCurrentWindowIdAsync() => this.SendMessageAsync(
        "GUI.GetProperties",
        new { Properties = new[] { "currentwindow" } },
        (JsonElement element) => element.GetProperty("currentwindow").GetProperty("id").GetInt32()
    );

    private async Task<PhysicalAddress> GetMacAddressAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            if (await this.GetLabelAsync("Network.MacAddress").ConfigureAwait(false) is { } label && PhysicalAddress.TryParse(label, out PhysicalAddress? macAddress))
            {
                return macAddress;
            }
        }
        return PhysicalAddress.None;
    }

    private Task<VolumeInfo> GetVolumeAsync() => this.SendMessageAsync(
        "Application.GetProperties",
        new { Properties = new[] { "muted", "volume" } },
        (VolumeInfo info) => info
    );

    private async Task MessageLoop()
    {
        byte[] previous = [];
        try
        {
            if (this._cancellationTokenSource is not { } cts || this._webSocket is not { State: WebSocketState.Open } webSocket)
            {
                return;
            }
            int previousLength = 0;
            using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(32768);
            while (webSocket.State == WebSocketState.Open && await webSocket.ReceiveAsync(owner.Memory, cts.Token).ConfigureAwait(false) is { MessageType: not WebSocketMessageType.Close } result)
            {
                if (previous.Length == 0 && result.EndOfMessage)
                {
                    // Complete message was received.
                    Process(owner.Memory.Span[0..result.Count]);
                    continue;
                }
                // Combine previous fragment with incoming one.
                int nextLength = previousLength + result.Count;
                byte[] next = ArrayPool<byte>.Shared.Rent(nextLength);
                if (previous.Length != 0)
                {
                    previous.AsSpan(0, previousLength).CopyTo(next);
                    ArrayPool<byte>.Shared.Return(previous);
                }
                owner.Memory[0..result.Count].CopyTo(next.AsMemory(previousLength));
                if (!result.EndOfMessage)
                {
                    (previous, previousLength) = (next, nextLength);
                    continue;
                }
                (previous, previousLength) = ([], 0);
                try
                {
                    Process(next.AsSpan(0, nextLength));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(next);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // KodiClient was disposed.
            return;
        }
        catch (WebSocketException)
        {
            this.OnDisconnected();
        }
        finally
        {
            if (previous.Length != 0)
            {
                ArrayPool<byte>.Shared.Return(previous);
            }
            this.CancelOutstanding();
        }

        void Process(ReadOnlySpan<byte> message)
        {
            JsonRpcResponse response = JsonSerializer.Deserialize<JsonRpcResponse>(message, KodiClient.SerializerOptions);
            if (response.Error is { Message: { } errorMessage })
            {
                this.Error?.Invoke(this, errorMessage);
            }
            else if (response is { Id: { } id } && this._taskSources.TryRemove(id, out TaskCompletionSource<JsonElement>? taskSource) && response.Result is { } element)
            {
                taskSource.TrySetResult(element);
            }
            else if (response is { Method: { } method, Parameters.Data: { } responseData })
            {
                this.ProcessIncomingMessage(method, responseData);
            }
        }
    }

    private async Task OnConnected()
    {
        if (this.MacAddress.Equals(PhysicalAddress.None))
        {
            this.MacAddress = await this.GetMacAddressAsync().ConfigureAwait(false);
        }
        this.Connected?.Invoke(this, EventArgs.Empty);
        this.ProcessVolumeInfo(await this.GetVolumeAsync().ConfigureAwait(false));
        // TODO: Query player state.
        this.PlayerState = PlayerState.Defaults;
    }

    private async void OnDisconnected()
    {
        this.CleanUp();
        this.PlayerState = PlayerState.Disconnected;
        this.Disconnected?.Invoke(this, EventArgs.Empty);
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(1d));
        while (!this.IsDisposed && !this.IsConnected && await timer.WaitForNextTickAsync().ConfigureAwait(false))
        {
            if (await this.ConnectAsync().ConfigureAwait(false))
            {
                return;
            }
        }
    }

    private async void OnPlay(PlayParameters parameters)
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
            (JsonElement element) => element.GetProperty("item")
        ).ConfigureAwait(false);
        this.PlayerState = parameters.Item.Type switch
        {
            ItemType.Picture when element.Deserialize<PictureInfo>(KodiClient.SerializerOptions) is { } picture => new(
               PlayState.Playing,
               picture.Label,
               parameters.Item.File ?? parameters.Item.ToString(),
               this.GetImageUrl(picture.Thumbnail)
            ),
            ItemType.Episode when element.Deserialize<EpisodeInfo>(KodiClient.SerializerOptions) is { } episode => CreatePlayerState(episode),
            ItemType.Song when element.Deserialize<SongInfo>(KodiClient.SerializerOptions) is { } song => CreatePlayerState(song),
            _ when element.Deserialize<VideoInfo>(KodiClient.SerializerOptions) is { } video => CreatePlayerState(video),

            _ => this.PlayerState,
        };

        PlayerState CreatePlayerState(IMediaInfo media) => new(
            PlayState.Playing,
            media.GetTitle(),
            media.GetDescription(),
            this.GetImageUrl(media.GetCoverArt())
        );
    }

    private void ProcessIncomingMessage(string method, JsonElement parameters)
    {
        switch (method)
        {
            case "Application.OnStop":
                this.PlayerState = PlayerState.Defaults;
                break;
            case "Application.OnVolumeChanged":
                this.ProcessVolumeInfo(parameters.Deserialize<VolumeInfo>(KodiClient.SerializerOptions));
                break;
            case "Player.OnPause":
                this.PlayerState = this.PlayerState with { PlayState = PlayState.Paused };
                break;
            case "Player.OnPlay":
                this.OnPlay(parameters.Deserialize<PlayParameters>(KodiClient.SerializerOptions));
                break;
        }
    }

    private void ProcessVolumeInfo(VolumeInfo info)
    {
        (int volume, bool muted) = info;
        this.Volume = volume;
        this.IsMuted = muted;
    }

    private Task<TResult> SendMessageAsync<TPayload, TResult>(string method, Func<TPayload, TResult> transform) => this.SendMessageAsync(
        method,
        default,
        transform
    );

    private Task<TResult> SendMessageAsync<TPayload, TResult>(string method, object? parameters, Func<TPayload, TResult> transform)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (this._webSocket is { State: WebSocketState.Open } webSocket)
        {
            return SendRequestAsync();
        }
        _ = this.ConnectAsync();
        return CreateCanceledTask();

        static Task<TResult> CreateCanceledTask() => Task.FromCanceled<TResult>(new(true));

        async Task<TResult> SendRequestAsync()
        {
            JsonRpcRequest request = new(method, parameters);
            TaskCompletionSource<JsonElement> elementSource = new();
            this._taskSources.TryAdd(request.Id, elementSource);
            await webSocket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(request, KodiClient.SerializerOptions).AsMemory(), WebSocketMessageType.Text, true, default).ConfigureAwait(false);
            // Each operation can take as much as 2.5s to complete.
            if (object.Equals(elementSource.Task, await Task.WhenAny(elementSource.Task, Task.Delay(2500)).ConfigureAwait(false)))
            {
                return transform(ExtractPayload(await elementSource.Task.ConfigureAwait(false)));
            }
            logger.LogWarning("Something went wrong (SendMessageAsync timed out)");
            return await CreateCanceledTask().ConfigureAwait(false);

            TPayload ExtractPayload(JsonElement element)
            {
                if (typeof(TPayload) == typeof(JsonElement))
                {
                    return Unsafe.As<JsonElement, TPayload>(ref element);
                }
                try
                {
                    return element.Deserialize<TPayload>(KodiClient.SerializerOptions)!;
                }
                catch (JsonException)
                {
                    logger.LogError("Failed to deserialize to {payload} from {element}.", typeof(TPayload), element);
                    throw;
                }
            }
        }
    }

    private void SetValue<TValue>(ref TValue field, TValue value, EventHandler<DataEventArgs<TValue>>? valueChanged)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        valueChanged?.Invoke(this, value);
    }

    private readonly record struct AlbumListResult(
        Limits Limits,
        AlbumInfo[] Albums
    );

    private readonly record struct EpisodeListResult(
        Limits Limits,
        EpisodeInfo[] Episodes
    );

    private readonly record struct ItemInfo(
        string? File,
        int Id,
        ItemType Type
    );

    private readonly record struct JsonRpcError(
        int Code,
        string Message
    );

    private readonly record struct JsonRpcRequest(string Method, [property: JsonPropertyName("params")] object? Parameters)
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        [JsonPropertyName("jsonrpc")]
        public string JsonRpcVersion { get; } = "2.0";
    }

    private readonly record struct JsonRpcResponse(
        JsonRpcError? Error = default,
        string? Id = default,
        string? Method = default,
        [property: JsonPropertyName("params")] ResponseParameters? Parameters = default,
        JsonElement? Result = default
    );

    private readonly record struct Limits(
        int Start,
        int End,
        int Total
    );

    private readonly record struct MoviesListResult(
        Limits Limits,
        VideoInfo[] Movies
    );

    private readonly record struct Notification(
        string Title,
        string Message,
        string? Image,
        [property: JsonPropertyName("displaytime")] int DisplayTime
    );

    private readonly record struct PictureInfo(
        string Label,
        string Thumbnail
    )
    {
        public static readonly string[] Fields = ["thumbnail", "title"];
    }

    private readonly record struct PlayerInfo(
        [property: JsonPropertyName("playerid")] int Id,
        int? Speed = default,
        PlayerType? Type = default
    );

    private readonly record struct PlayParameters(
        ItemInfo Item,
        PlayerInfo Player
    );

    private readonly record struct ResponseParameters(
        JsonElement Data,
        string Sender
    );

    private readonly record struct SeekResult(
        double Percentage,
        Time Time,
        Time TotalTime
    );

    private readonly record struct SortOrder(
        string Method,
        string Order = "ascending",
        [property: JsonPropertyName("ignorearticle")] bool IgnoreArticle = true
    );

    private readonly record struct Time(
        int Hours,
        int Minutes,
        int Seconds,
        int Milliseconds
    );

    private readonly record struct TVShowsListResult(
        Limits Limits,
        [property: JsonPropertyName("tvshows")] TVShowInfo[] TVShows
    );

    private readonly record struct VolumeInfo(
        int Volume,
        bool Muted = false
    );
}
