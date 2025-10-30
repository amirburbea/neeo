using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

internal class PlexServerConnection(
    string serverName,
    IPlexDiscovery discovery,
    IPlexTokenStore tokenStore,
    HttpClient httpClient,
    ILogger logger
) : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Lock _lock = new();
    private Task? _serverMessageLoopTask;
    private Task<WebSocket>? _notificationsSocketTask;

    private CancellationToken CancellationToken => this._cancellationTokenSource.Token;
    private IPAddress IPAddress => discovery.Servers[serverName].IPAddress;

    public PlexServerInfo Info => discovery.Servers.GetValueOrDefault(serverName);

    public PlexPlayerInfo? SelectedPlayer { get; private set; }

    public EventHandler<DataEventArgs<PlexPlayerInfo>>? SelectedPlayerChanged;

    public string ServerName => serverName;

    public void Dispose()
    {
        using CancellationTokenSource cts = this._cancellationTokenSource;
        cts.Cancel();
        try
        {
            this._serverMessageLoopTask?.Wait();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogDebug(e, "Error waiting for message loop to exit during Dispose.");
        }
        if (this._notificationsSocketTask is { IsCompletedSuccessfully: true } task)
        {
            try
            {
                task.Result.Dispose();
            }
            catch (Exception e)
            {
                logger.LogDebug(e, "Error closing WebSocket during Dispose.");
            }
        }
    }

    public async Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken)
    {
        string uri = $"http://{this.IPAddress}:32400/clients";
        ClientsResponse response = await httpClient.GetAsync<ClientsResponse>(
            new(uri),
            request => request.Headers.Add("X-Plex-Token", tokenStore.AuthToken),
            cancellationToken
        ).ConfigureAwait(false);
        if (response.MediaContainer.Clients is not { Length: > 0 } clients)
        {
            return [];
        }
        return [..
            clients
                .Where(client => (client.ProtocolCapabilities & ProtocolCapabilities.Playback) != 0)
                .Select(client => new PlexPlayerInfo(client.Name, client.MachineIdentifier))
        ];
    }

    public async Task<WebSocket> InitializeAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this._cancellationTokenSource.IsCancellationRequested, nameof(PlexServerConnection));
        if (this._notificationsSocketTask is null)
        {
            using (this._lock.EnterScope())
            {
                this._notificationsSocketTask ??= CreateConnectedSocketAsync(cancellationToken);
            }
        }
        WebSocket webSocket;
        try
        {
            webSocket = await this._notificationsSocketTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Connection failed, clear the task so the next call can try again.
            using (this._lock.EnterScope())
            {
                if (this._notificationsSocketTask is { IsFaulted: true })
                {
                    this._notificationsSocketTask = null;
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
                    this._serverMessageLoopTask = this.ServerMessageLoop(webSocket);
                }
            }
        }
        return webSocket;

        async Task<WebSocket> CreateConnectedSocketAsync(CancellationToken cancellationToken)
        {
            // Disable keep-alive pings, Plex server does not support them.
            ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
            string url = $"ws://{this.IPAddress}:32400/:/websockets/notifications";
            try
            {
                webSocket.Options.SetRequestHeader("X-Plex-Token", tokenStore.AuthToken);
                await webSocket.ConnectAsync(new(url), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error connecting to Plex server WebSocket at {url}.", url);
                webSocket.Dispose();
                throw;
            }
            return webSocket;
        }
    }

    public async Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        if (this.SelectedPlayer?.MachineIdentifier == machineIdentifier)
        {
            return;
        }
        PlexPlayerInfo[] players = await this.GetPlayersAsync(cancellationToken).ConfigureAwait(false);
        int index = Array.FindIndex(players, player => player.MachineIdentifier == machineIdentifier);
        if (index < 0)
        {
            return;
        }
        PlexPlayerInfo info = players[index];
        this.SelectedPlayer = info;
        this.SelectedPlayerChanged?.Invoke(this, info);
    }

    private Task ProcessNotificationsAsync(ServerNotificationContainer container)
    {
        return container.Type switch
        {
            ServerNotificationType.Activity when container.ActivityNotifications is { } notifications => ProcessActivityNotificationsAsync(notifications),
            ServerNotificationType.Playing when container.PlaySessionStateNotifications is { } notifications => ProcessPlayStateNotificationsAsync(notifications),
            ServerNotificationType.Timeline when container.TimelineEntries is { } entries => ProcessTimelineEntriesAsync(entries),




            _ => Task.CompletedTask
        };



        async Task ProcessActivityNotificationsAsync(ActivityNotification[] notifications)
        {
        }

        async Task ProcessPlayStateNotificationsAsync(PlaySessionStateNotification[] notifications)
        {
        }

        async Task ProcessTimelineEntriesAsync(TimelineEntry[] entries)
        {
        }
    }

    private async Task ServerMessageLoop(WebSocket webSocket)
    {
        while (!this.CancellationToken.IsCancellationRequested)
        {
            // If we don't have a connected socket, try to connect (or wait for connection)
            if (webSocket.State != WebSocketState.Open)
            {
                logger.LogInformation("Attempting to reconnect to Plex server {Name}", serverName);
                try
                {
                    // Clear the old failed socket task reference
                    using (this._lock.EnterScope())
                    {
                        this._notificationsSocketTask = null;
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
            byte[] previous = [];
            try
            {
                int previousLength = 0;
                using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(32768);
                while (webSocket.State == WebSocketState.Open && !this.CancellationToken.IsCancellationRequested)
                {
                    ValueWebSocketReceiveResult result = await webSocket.ReceiveAsync(owner.Memory, this.CancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Server closed the connection.
                        return;
                    }
                    // If a complete message was received in a single read, process it.
                    if (result.EndOfMessage && previous is [])
                    {
                        await ProcessAsync(owner.Memory.Span[..result.Count]).ConfigureAwait(false);
                        continue;
                    }
                    // Combine previous fragment with incoming one.
                    int nextLength = previousLength + result.Count;
                    byte[] next = ArrayPool<byte>.Shared.Rent(nextLength);
                    if (previous is not [])
                    {
                        previous.AsSpan(0, previousLength).CopyTo(next);
                        ArrayPool<byte>.Shared.Return(previous);
                    }
                    owner.Memory[..result.Count].CopyTo(next.AsMemory(previousLength));
                    // If we still did not receive the complete message, store the fragment.
                    if (!result.EndOfMessage)
                    {
                        (previous, previousLength) = (next, nextLength);
                        continue;
                    }
                    // We received the complete message, process it and reset fragment storage.
                    (previous, previousLength) = ([], 0);
                    try
                    {
                        await ProcessAsync(next.AsSpan(0, nextLength)).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(next);
                    }
                }
            }
            catch (OperationCanceledException) when (this.CancellationToken.IsCancellationRequested)
            {
                // Cancellation requested, exit cleanly
                return;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Message loop failed for Plex server {Name}. Attempting reconnection in 5 seconds.", serverName);
            }
            finally
            {
                // Always return any rented buffer on exit
                if (previous is not [])
                {
                    ArrayPool<byte>.Shared.Return(previous);
                }
            }
            // 3. Clean up the failed connection before looping to reconnect
            logger.LogInformation("Disconnected from Plex server {Name}", serverName);
            using (this._lock.EnterScope())
            {
                this._notificationsSocketTask = null;
            }
            webSocket.Dispose();
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

        Task ProcessAsync(ReadOnlySpan<byte> span)
        {
            return JsonSerializer.Deserialize<ServerMessage?>(span, JsonSerialization.Options) is { Notifications: { } notifications }
                ? this.ProcessNotificationsAsync(notifications)
                : Task.CompletedTask;
        }
    }

    private record struct Activity(
        [property: JsonPropertyName("cancellable")] bool Cancelable,
        int Progress,
        string Subtitle,
        string Title,
        string Type,
        [property: JsonPropertyName("userID")] int UserId,
        string Uuid
    );

    private record struct ActivityNotification(
        [property: JsonPropertyName("Activity")] Activity Activity,
        string Event,
        string Uuid
    );


    private record struct ClientServer(
        string Name,
        string Host,
        string Address,
        int Port,
        string MachineIdentifier,
        string Version,
        string Protocol,
        string Product,
        string DeviceClass,
        double ProtocolVersion,
        ProtocolCapabilities ProtocolCapabilities
    );

    private record struct ClientsMediaContainer(
        [property: JsonPropertyName("Server")] ClientServer[]? Clients
    );

    private record struct ClientsResponse(
        [property: JsonPropertyName("MediaContainer")]
        ClientsMediaContainer MediaContainer
    );

    private record struct ServerNotificationContainer(
        int Size,
        [property: JsonPropertyName("Type")] ServerNotificationType Type,
        /// <summary>Background tasks, library scans, etc...</summary>
        [property: JsonPropertyName("ActivityNotification")] ActivityNotification[]? ActivityNotifications,
        /// <summary>Playback state changes (play, pause, stop, buffer)</summary>
        [property: JsonPropertyName("PlaySessionStateNotification")] PlaySessionStateNotification[]? PlaySessionStateNotifications,
        /// <summary>Server reachability status</summary>
        [property: JsonPropertyName("ReachabilityNotification")] ReachabilityNotification[]? ReachabilityNotifications,
        /// <summary>Server status changes</summary>
        [property: JsonPropertyName("StatusNotification")] StatusNotification[]? StatusNotifications,
        /// <summary>More detailed timeline/state updates for media items</summary>
        [property: JsonPropertyName("TimelineEntry")] TimelineEntry[]? TimelineEntries
    );

    private record struct PlaySessionStateNotification(
        string ClientIdentifier,
        string Guid,
        string Key,
        [property: JsonPropertyName("playQueueID")] int PlayQueueId,
        [property: JsonPropertyName("playQueueItemID")] int PlayQueueItemId,
        string RatingKey,
        string SessionKey,
        string State,
        string Url,
        int ViewOffset,
        string? TranscodeSession, // UUID if transcoding
        string? PlayerId,
        string? MachineIdentifier
    );

    [Flags, JsonConverter(typeof(TextJsonConverter<ProtocolCapabilities>))]
    private enum ProtocolCapabilities
    {
        None = 0,

        [Text("playback")]
        Playback = 1,

        [Text("navigation")]
        Navigation = 2,

        [Text("timeline")]
        Timeline = 4,

        [Text("playqueues")]
        Playqueues = 8,

        [Text("provider-playback")]
        ProviderPlayback = 16,
    }

    private record struct ReachabilityNotification(
        bool Reachability
    );

    private record struct ServerMessage(
        [property: JsonPropertyName("NotificationContainer")] ServerNotificationContainer? Notifications
    );

    private record struct StatusNotification(
        string Description,
        string NotificationName,
        string Title
    );

    private record struct TimelineEntry(
        string Identifier,
        [property: JsonPropertyName("itemID")] int ItemId,
        string MetadataState,
        [property: JsonPropertyName("sectionID")] int SectionId,
        int State,
        int Type,
        long UpdatedAt,
        string? Title
    );

    [JsonConverter(typeof(TextJsonConverter<ServerNotificationType>))]
    private enum ServerNotificationType
    {
        Unknown = 0,

        [Text("playing")]
        Playing,

        [Text("reachability")]
        Reachability,

        [Text("preference")]
        Preference,

        [Text("update.statechange")]
        StateChange,

        [Text("activity")]
        Activity,

        [Text("timeline")]
        Timeline,

        [Text("backgroundProcessingQueue")]
        BackgroundProcessingQueue,

        [Text("transcodeSession.start")]
        TranscodeSessionStart,

        [Text("transcodeSession.update")]
        TranscodeSessionUpdate,

        [Text("transcodeSession.end")]
        TranscodeSessionEnd,
    }
}
