using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class JsonWebSocketTests : IAsyncLifetime
{
    private TcpListener? _listener;

    public async ValueTask InitializeAsync()
    {
        this._listener = new(IPAddress.Loopback, 0);
        this._listener.Start();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        this._listener?.Stop();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task MessageLoop_processes_a_single_complete_message()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (WebSocket client, WebSocket server) = await this.CreateWebSocketPairAsync(cancellationToken);
        List<TestMessage> received = [];

        Task loopTask = Neeo.Sdk.Utilities.JsonWebSocket.MessageLoop<TestMessage>(
            client,
            (message, _) =>
            {
                received.Add(message);
                return ValueTask.CompletedTask;
            },
            cancellationToken: cancellationToken
        );

        await SendJsonAsync(server, new TestMessage("hello"), cancellationToken);
        await JsonWebSocketTests.WaitUntilAsync(() => received.Count > 0, cancellationToken);
        await server.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
        await loopTask;

        Assert.Equal([new TestMessage("hello")], received);
    }

    [Fact]
    public async Task MessageLoop_reassembles_a_message_fragmented_across_multiple_frames()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (WebSocket client, WebSocket server) = await this.CreateWebSocketPairAsync(cancellationToken);
        List<TestMessage> received = [];

        Task loopTask = Neeo.Sdk.Utilities.JsonWebSocket.MessageLoop<TestMessage>(
            client,
            (message, _) =>
            {
                received.Add(message);
                return ValueTask.CompletedTask;
            },
            cancellationToken: cancellationToken
        );

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new TestMessage("fragmented-value"), JsonSerializerOptions.Web);
        // Split into three small frames instead of sending the whole payload at once.
        int third = payload.Length / 3;
        await server.SendAsync(payload.AsMemory(0, third), WebSocketMessageType.Text, endOfMessage: false, cancellationToken);
        await server.SendAsync(payload.AsMemory(third, third), WebSocketMessageType.Text, endOfMessage: false, cancellationToken);
        await server.SendAsync(payload.AsMemory(third * 2), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        await JsonWebSocketTests.WaitUntilAsync(() => received.Count > 0, cancellationToken);
        await server.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
        await loopTask;

        Assert.Equal([new TestMessage("fragmented-value")], received);
    }

    [Fact]
    public async Task MessageLoop_processes_multiple_messages_in_order()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (WebSocket client, WebSocket server) = await this.CreateWebSocketPairAsync(cancellationToken);
        List<TestMessage> received = [];

        Task loopTask = Neeo.Sdk.Utilities.JsonWebSocket.MessageLoop<TestMessage>(
            client,
            (message, _) =>
            {
                received.Add(message);
                return ValueTask.CompletedTask;
            },
            cancellationToken: cancellationToken
        );

        await SendJsonAsync(server, new TestMessage("first"), cancellationToken);
        await SendJsonAsync(server, new TestMessage("second"), cancellationToken);
        await SendJsonAsync(server, new TestMessage("third"), cancellationToken);
        await JsonWebSocketTests.WaitUntilAsync(() => received.Count >= 3, cancellationToken);
        await server.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
        await loopTask;

        Assert.Equal([new TestMessage("first"), new TestMessage("second"), new TestMessage("third")], received);
    }

    [Fact]
    public async Task MessageLoop_invokes_onDisconnected_on_graceful_close()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (WebSocket client, WebSocket server) = await this.CreateWebSocketPairAsync(cancellationToken);
        int disconnectedCount = 0;

        Task loopTask = Neeo.Sdk.Utilities.JsonWebSocket.MessageLoop<TestMessage>(
            client,
            static (_, _) => ValueTask.CompletedTask,
            onDisconnected: () => Interlocked.Increment(ref disconnectedCount),
            cancellationToken: cancellationToken
        );

        await server.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
        await loopTask;

        Assert.Equal(1, disconnectedCount);
    }

    [Fact]
    public async Task MessageLoop_notifies_and_rethrows_when_processMessage_throws()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (WebSocket client, WebSocket server) = await this.CreateWebSocketPairAsync(cancellationToken);
        int disconnectedCount = 0;

        Task loopTask = Neeo.Sdk.Utilities.JsonWebSocket.MessageLoop<TestMessage>(
            client,
            static (_, _) => throw new InvalidOperationException("Simulated failure in processMessage."),
            onDisconnected: () => Interlocked.Increment(ref disconnectedCount),
            cancellationToken: cancellationToken
        );

        await SendJsonAsync(server, new TestMessage("boom"), cancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => loopTask);

        Assert.Equal(1, disconnectedCount);
    }

    private async Task<(WebSocket Client, WebSocket Server)> CreateWebSocketPairAsync(CancellationToken cancellationToken)
    {
        TcpListener listener = this._listener!;
        Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync(cancellationToken).AsTask();
        // Deliberately not disposed here - the TcpClient instances must outlive this method, since the
        // WebSockets returned below wrap their streams and are used well after this method returns.
        TcpClient client = new();
        await client.ConnectAsync((IPEndPoint)listener.LocalEndpoint, cancellationToken);
        TcpClient server = await acceptTask;
        WebSocket clientSocket = WebSocket.CreateFromStream(client.GetStream(), isServer: false, subProtocol: null, keepAliveInterval: TimeSpan.Zero);
        WebSocket serverSocket = WebSocket.CreateFromStream(server.GetStream(), isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.Zero);
        return (clientSocket, serverSocket);
    }

    private static Task SendJsonAsync(WebSocket socket, TestMessage message, CancellationToken cancellationToken)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonSerializerOptions.Web);
        return socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        for (int i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
        Assert.True(condition(), "Timed out waiting for the expected condition.");
    }

    public readonly record struct TestMessage(string Value);
}
