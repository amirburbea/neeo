using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.WebOS;

public sealed class WebOSPointerSocket(Uri uri, ILogger? logger = null) : IDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private ClientWebSocket? _webSocket;
    private bool _isDisposed;

    public bool IsConnected => this._webSocket is { State: WebSocketState.Open };

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        this._webSocket?.Dispose();
        ClientWebSocket socket = new();
        if (uri.Scheme == Uri.UriSchemeWss)
        {
            // The TV presents a self-signed certificate; this only applies to this socket.
            socket.Options.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
        }
        try
        {
            await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            this._webSocket = socket;
            logger?.LogDebug("Connected pointer socket to {uri}", uri);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public Task SendButtonAsync(string buttonName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(buttonName);
        return this.SendCommandAsync($"type:button\nname:{buttonName}\n\n", cancellationToken);
    }

    public Task ClickAsync(CancellationToken cancellationToken = default)
    {
        return this.SendCommandAsync("type:click\n\n", cancellationToken);
    }

    public Task MoveAsync(int dx, int dy, bool down = false, CancellationToken cancellationToken = default)
    {
        return this.SendCommandAsync($"type:move\ndx:{dx}\ndy:{dy}\ndown:{(down ? 1 : 0)}\n\n", cancellationToken);
    }

    public Task ScrollAsync(int dx, int dy, CancellationToken cancellationToken = default)
    {
        return this.SendCommandAsync($"type:scroll\ndx:{dx}\ndy:{dy}\n\n", cancellationToken);
    }

    private async Task SendCommandAsync(string text, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        if (this._webSocket is not { State: WebSocketState.Open } socket)
        {
            throw new InvalidOperationException("Pointer socket is not connected.");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await this._sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this._sendLock.Release();
        }
    }

    public void Dispose()
    {
        if (this._isDisposed)
        {
            return;
        }
        this._isDisposed = true;
        this._webSocket?.Dispose();
        this._sendLock.Dispose();
    }
}
