using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Neeo.Drivers.Kodi.Tests;

/// <summary>
/// A minimal Kodi JSON-RPC WebSocket server on a random loopback port.
/// </summary>
internal sealed class FakeKodiServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FakeKodiServer(WebApplication app, Func<WebSocket, string, int, Task> onRequest)
    {
        this._app = app;
        this.Port = new Uri(app.Urls.First()).Port;
        _ = onRequest;
    }

    public int Port { get; }

    /// <param name="onRequest">Called for each request received: the socket, the raw request JSON and the 1-based request number.</param>
    public static async Task<FakeKodiServer> StartAsync(Func<WebSocket, string, int, Task> onRequest)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        WebApplication app = builder.Build();
        app.UseWebSockets();
        app.Map("/jsonrpc", async context =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            byte[] buffer = new byte[16 * 1024];
            int count = 0;
            while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await socket.ReceiveAsync(buffer, context.RequestAborted);
                }
                catch (Exception)
                {
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                await onRequest(socket, Encoding.UTF8.GetString(buffer, 0, result.Count), ++count);
            }
        });
        await app.StartAsync();
        return new(app, onRequest);
    }

    /// <summary>
    /// Sends a JSON-RPC success response carrying <paramref name="resultJson"/> for the request.
    /// </summary>
    public static Task RespondAsync(WebSocket socket, string requestJson, string resultJson)
    {
        using JsonDocument request = JsonDocument.Parse(requestJson);
        string id = request.RootElement.GetProperty("id").GetRawText();
        return socket.SendAsync(Encoding.UTF8.GetBytes($$"""{"jsonrpc":"2.0","id":{{id}},"result":{{resultJson}}}"""), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async ValueTask DisposeAsync() => await this._app.DisposeAsync();
}
