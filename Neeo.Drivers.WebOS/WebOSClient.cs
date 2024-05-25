using System;
using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.WebOS;

public sealed class WebOSClient(IPAddress ipAddress) : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task<bool>? _connectTask;
    private ClientWebSocket? _webSocket;

    public event EventHandler? Connected;

    public event EventHandler? Disconnected;

    private event EventHandler? Disposed;

    public IPAddress IPAddress { get; } = ipAddress;

    public bool IsConnected => this._webSocket is { State: WebSocketState.Open };

    public bool IsDisposed { get; private set; }

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (this._connectTask == null)
        {
            (this._connectTask = ConnectAsync()).ContinueWith(_ => this._connectTask = null, TaskContinuationOptions.ExecuteSynchronously);
        }
        return this._connectTask;

        async Task<bool> ConnectAsync()
        {
            ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = TimeSpan.FromSeconds(30d) } };
            CancellationTokenSource cts = new();
            try
            {
                await webSocket.ConnectAsync(new($"ws://{this.IPAddress}:3000"), cancellationToken).ConfigureAwait(false);
                this._cancellationTokenSource = cts;
                this._webSocket = webSocket;
                _ = Task.Factory.StartNew(this.MessageLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                this.OnConnected();
                return true;
            }
            catch (WebSocketException)
            {
                cts.Dispose();
                webSocket.Dispose();
                return false;
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

    private void CleanUp()
    {
        using WebSocket? webSocket = Interlocked.Exchange(ref this._webSocket, default);
        using CancellationTokenSource? source = Interlocked.Exchange(ref this._cancellationTokenSource, default);
        source?.Cancel();
    }

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
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(8192);
            while (webSocket.State == WebSocketState.Open && await webSocket.ReceiveAsync(buffer.Memory, cts.Token).ConfigureAwait(false) is { } result)
            {
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                if (previous.Length == 0 && result.EndOfMessage)
                {
                    // Complete message was received.
                    Process(buffer.Memory.Span[0..result.Count]);
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
                buffer.Memory.Span[0..result.Count].CopyTo(next.AsSpan(previousLength));
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
            //this.CancelOutstanding();
        }

        static void Process(ReadOnlySpan<byte> message)
        {
            //JsonRpcResponse response = JsonSerializer.Deserialize<JsonRpcResponse>(message, JsonRpc.SerializerOptions);
            //if (response.Error is { Message: { } errorMessage })
            //{
            //    this.Error?.Invoke(this, errorMessage);
            //}
            //else if (response.Id is { } id && this._taskSources.TryRemove(id, out TaskCompletionSource<JsonElement>? taskSource) && response.Result is { } element)
            //{
            //    taskSource.TrySetResult(element);
            //}
            //else if (response.Method is { } method && response.Parameters is { } parameters)
            //{
            //    this.ProcessIncomingMessage(response.Method, parameters.Data);
            //}
        }
    }

    private void OnConnected()
    {
        this.Connected?.Invoke(this, EventArgs.Empty);
    }

    private async void OnDisconnected()
    {
        this.CleanUp();
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
}
