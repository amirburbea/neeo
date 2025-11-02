using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

public interface IPlexServer : IDisposable
{
    MediaItem? ActiveMedia { get; }
    IObservable<MediaItem?> ActiveMediaChanged { get; }
    IObservable<Unit> Disposed { get; }
    PlexServerInfo Info { get; }
    bool IsConnected { get; }
    string Name { get; }
    PlayState PlayState { get; }
    IObservable<PlayState> PlayStateChanged { get; }
    PlexPlayerInfo? SelectedPlayer { get; }
    IObservable<PlexPlayerInfo?> SelectedPlayerChanged { get; }

    Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken = default);

    Task SendPlayerCommandAsync(string command, CancellationToken cancellationToken = default);
}

internal sealed partial class PlexServer(
    string name,
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    HttpClient httpClient,
    IPlexSettingsManager settingsManager,
    ILogger logger
) : IPlexServer, IDisposable
{
    private readonly BehaviorSubject<MediaItem?> _activeMedia = new(null);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Subject<Unit> _disposed = new();
    private readonly Lock _lock = new();
    private readonly BehaviorSubject<PlayState> _playState = new(PlayState.Stopped);
    private readonly BehaviorSubject<PlexPlayerInfo?> _selectedPlayer = new(null);
    private int _commandId;
    private Task? _serverMessageLoopTask;
    private Task<WebSocket>? _webSocketTask;

    public MediaItem? ActiveMedia => this._activeMedia.Value;

    public IObservable<MediaItem?> ActiveMediaChanged => this._activeMedia;

    public IObservable<Unit> Disposed => this._disposed;

    public PlexServerInfo Info => discovery.Servers.GetValueOrDefault(name);

    public bool IsConnected => this._serverMessageLoopTask is { Status: TaskStatus.Running };

    public string Name => name;

    public PlayState PlayState => this._playState.Value;

    public IObservable<PlayState> PlayStateChanged => this._playState;

    public PlexPlayerInfo? SelectedPlayer => this._selectedPlayer.Value;

    public IObservable<PlexPlayerInfo?> SelectedPlayerChanged => this._selectedPlayer;

    private IPAddress IPAddress => discovery.Servers[name].IPAddress;

    private CancellationToken CancellationToken => this._cancellationTokenSource.Token;

    public void Dispose()
    {
        this._cancellationTokenSource.Cancel();
        try
        {
            this._serverMessageLoopTask?.Wait();
        }
        catch (Exception)
        {
            // Ignore.
        }
        finally
        {
            this._activeMedia.Dispose();
            this._selectedPlayer.Dispose();
            if (this._webSocketTask is { Status: TaskStatus.RanToCompletion } and { Result: { } socket })
            {
                socket.Dispose();
            }
            this._disposed.OnNext(default);
            this._disposed.Dispose();
            this._cancellationTokenSource.Dispose();
        }
    }

    public async Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetAsync<Response<ClientsMediaContainer>>(this.GetUri("clients"), this.AddTokenHeaders, cancellationToken).ConfigureAwait(false) switch
        {
            { MediaContainer.Clients: { Length: not 0 } clients } => [..
                clients
                    .Where(client => (client.ProtocolCapabilities & PlayerCapabilities.Playback) != 0)
                    .Select(client => new PlexPlayerInfo(
                        client.Name,
                        client.MachineIdentifier,
                        client.ProtocolCapabilities,
                        IPAddress.Parse(client.Address),
                        client.Port,
                        client.Product
                    ))
            ],
            _ => []
        };
    }

    public async Task<WebSocket> InitializeAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this._cancellationTokenSource.IsCancellationRequested, nameof(PlexServer));
        WebSocket webSocket;
        try
        {
            webSocket = await GetSocketAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Connection failed, clear the task so the next call to InitializeAsync can try again.
            using (this._lock.EnterScope())
            {
                if (this._webSocketTask is { IsFaulted: true })
                {
                    this._webSocketTask = null;
                }
            }
            throw;
        }
        if (this._serverMessageLoopTask is not { Status: TaskStatus.Running })
        {
            using (this._lock.EnterScope())
            {
                if (this._serverMessageLoopTask is not { Status: TaskStatus.Running })
                {
                    this._serverMessageLoopTask = this.MessageLoop(webSocket);
                }
            }
        }
        return webSocket;

        Task<WebSocket> GetSocketAsync()
        {
            if (this._webSocketTask is { } task)
            {
                return task;
            }
            using (this._lock.EnterScope())
            {
                return this._webSocketTask ??= CreateConnectedSocketAsync();
            }

            async Task<WebSocket> CreateConnectedSocketAsync()
            {
                // Disable keep-alive pings, Plex server does not support them.
                ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
                try
                {
                    webSocket.Options.SetRequestHeader("X-Plex-Token", tokenStore.AuthToken);
                    await webSocket.ConnectAsync(this.GetUri(":/websockets/notifications", "ws"), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error connecting to Plex server WebSocket at {IP}.", this.IPAddress);
                    webSocket.Dispose();
                    throw;
                }
                return webSocket;
            }
        }
    }

    public async Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (this.SelectedPlayer?.MachineIdentifier == machineIdentifier)
        {
            return;
        }
        PlexPlayerInfo[] players = await this.GetPlayersAsync(cancellationToken).ConfigureAwait(false);
        if (Array.FindIndex(players, player => player.MachineIdentifier == machineIdentifier) is int index and not -1)
        {
            await OnPlayerSelectedAsync(players[index]).ConfigureAwait(false);
        }

        async Task OnPlayerSelectedAsync(PlexPlayerInfo player)
        {
            this._selectedPlayer.OnNext(player);
            JsonElement result = await httpClient.GetAsync<JsonElement>(this.GetUri("status/sessions"), this.AddTokenHeaders, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SendPlayerCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (this.SelectedPlayer is not { MachineIdentifier: { } identifier, Capabilities: { } })
        {
            return;
        }
        Uri uri = this.GetUri($"{command}?commandID={Interlocked.Increment(ref this._commandId)}&machineIdentifier={identifier}");
        HttpResponseMessage response = await httpClient.PostAsync(uri, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Plex command failed ({Status}): {Uri}", response.StatusCode, uri);
        }
    }

    private void AddTokenHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Plex-Token", tokenStore.AuthToken);
        request.Headers.Add("X-Plex-Client-Identifier", tokenStore.ClientIdentifier);
    }

    private Uri GetUri(string pathSuffix, string protocol = "http") => new($"{protocol}://{this.IPAddress}:32400/{pathSuffix}");

    private async Task MessageLoop(WebSocket webSocket)
    {
        while (!this.CancellationToken.IsCancellationRequested)
        {
            // If we don't have a connected socket, try to connect (or wait for connection)
            if (webSocket.State != WebSocketState.Open)
            {
                logger.LogInformation("Attempting to reconnect to Plex server {Name}", name);
                try
                {
                    // Clear the old failed socket task reference
                    using (this._lock.EnterScope())
                    {
                        this._webSocketTask = null;
                    }
                    // InitializeAsync will try to establish a new connection and update _socketTask
                    webSocket = await this.InitializeAsync(this.CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (this.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception)
                {
                    try
                    {
                        // Connection failed, wait before trying again
                        await Task.Delay(TimeSpan.FromSeconds(5), this.CancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
            // Process any received messages.
            await JsonWebSocket.MessageLoop<ServerMessage>(
                 webSocket,
                 (message, _) =>
                 {
                     return message.Notifications is { } notifications
                        ? new(this.ProcessNotificationsAsync(notifications))
                        : ValueTask.CompletedTask;
                 },
                 () => logger.LogWarning("Disconnected, waiting to reconnect."),
                 this.CancellationToken
             ).ConfigureAwait(false);
            // Clean up the failed connection before looping to reconnect
            logger.LogInformation("Disconnected from Plex server {Name}", name);
            using (this._lock.EnterScope())
            {
                this._webSocketTask = null;
            }
            webSocket.Dispose();
            if (this.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                // Wait before trying to reconnect to avoid spamming the server.
                await Task.Delay(TimeSpan.FromSeconds(5), this.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private Task ProcessNotificationsAsync(ServerNotificationContainer container) => (container, this.SelectedPlayer?.MachineIdentifier) switch
    {
        ({ Type: ServerNotificationType.Playing, PlaySessionStateNotifications: { } notifications }, { } clientId) => Parallel.ForEachAsync(
            notifications.Where(notification => notification.ClientIdentifier == clientId),
            this.CancellationToken,
            this.ProcessPlayNotificationAsync
        ),
        _ => Task.CompletedTask,
    };

    private async ValueTask ProcessPlayNotificationAsync(PlaySessionStateNotification notification, CancellationToken cancellationToken)
    {
        this._playState.OnNext(notification.State);
    }
}

internal sealed class PlexServerConsumer(PlexServer server) : IPlexServer, IDisposable
{
    private readonly Subject<Unit> _disposed = new();

    public IObservable<Unit> Disposed => this._disposed;
    public MediaItem? ActiveMedia => server.ActiveMedia;
    public IObservable<MediaItem?> ActiveMediaChanged => server.ActiveMediaChanged;
    public PlexServerInfo Info => server.Info;
    public bool IsConnected => server.IsConnected;
    public string Name => server.Name;
    public PlayState PlayState => server.PlayState;
    public IObservable<PlayState> PlayStateChanged => server.PlayStateChanged;
    public PlexPlayerInfo? SelectedPlayer => server.SelectedPlayer;
    public IObservable<PlexPlayerInfo?> SelectedPlayerChanged => server.SelectedPlayerChanged;

    public void Dispose()
    {
        this._disposed.OnNext(default);
        this._disposed.Dispose();
    }

    public Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken) => server.GetPlayersAsync(cancellationToken);

    public Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken) => server.SelectPlayerAsync(machineIdentifier, cancellationToken);

    public Task SendPlayerCommandAsync(string command, CancellationToken cancellationToken) => server.SendPlayerCommandAsync(command, cancellationToken);
}
