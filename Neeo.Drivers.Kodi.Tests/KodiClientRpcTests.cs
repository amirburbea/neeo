using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Drivers.Kodi.Models;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

public sealed class KodiClientRpcTests
{
    [Fact]
    public async Task A_request_fails_with_a_TimeoutException_when_kodi_does_not_answer()
    {
        await using FakeKodiServer server = await FakeKodiServer.StartAsync((_, _, _) => Task.CompletedTask);
        using KodiClient client = await KodiClientRpcTests.ConnectAsync(server.Port, TimeSpan.FromMilliseconds(150));

        await Assert.ThrowsAsync<TimeoutException>(() => client.GetPlayersAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_request_returns_the_result_when_kodi_answers()
    {
        await using FakeKodiServer server = await FakeKodiServer.StartAsync((socket, request, _) => FakeKodiServer.RespondAsync(socket, request, "[]"));
        using KodiClient client = await KodiClientRpcTests.ConnectAsync(server.Port, TimeSpan.FromSeconds(5));

        PlayerDescriptor[] players = await client.GetPlayersAsync(TestContext.Current.CancellationToken);

        Assert.Empty(players);
    }

    [Fact]
    public async Task A_request_fails_with_a_WebSocketException_when_the_connection_drops_while_waiting()
    {
        // Answers the first request (made while connecting), then closes the connection on the next one.
        await using FakeKodiServer server = await FakeKodiServer.StartAsync(async (socket, request, number) =>
        {
            if (number == 1)
            {
                await FakeKodiServer.RespondAsync(socket, request, "{}");
                return;
            }
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        });
        using KodiClient client = await KodiClientRpcTests.ConnectAsync(server.Port, TimeSpan.FromSeconds(10));

        // Fails with the dropped connection, not by waiting out the (long) request timeout.
        WebSocketException exception = await Assert.ThrowsAsync<WebSocketException>(() => client.GetPlayersAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WebSocketError.ConnectionClosedPrematurely, exception.WebSocketErrorCode);
    }

    [Fact]
    public async Task A_request_is_cancelled_when_the_caller_cancels()
    {
        await using FakeKodiServer server = await FakeKodiServer.StartAsync((_, _, _) => Task.CompletedTask);
        using KodiClient client = await KodiClientRpcTests.ConnectAsync(server.Port, TimeSpan.FromSeconds(10));
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(150));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetPlayersAsync(cts.Token));
    }

    [Fact]
    public async Task A_request_fails_with_an_InvalidOperationException_when_not_connected()
    {
        // Nothing listens on port 1.
        using KodiClient client = new("Kodi", IPAddress.Loopback, 8080, new RecordingLogger(), webSocketPort: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetPlayersAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Connects a client to the fake server, then applies <paramref name="requestTimeout"/> for the requests under test (connecting
    /// itself makes a request, which would otherwise wait out that timeout when the fake server is silent).
    /// </summary>
    private static async Task<KodiClient> ConnectAsync(int webSocketPort, TimeSpan requestTimeout)
    {
        KodiClient client = new("Kodi", IPAddress.Loopback, 8080, new RecordingLogger(), webSocketPort)
        {
            RequestTimeout = TimeSpan.FromMilliseconds(100),
        };
        Assert.True(await client.ConnectAsync(TestContext.Current.CancellationToken));
        client.RequestTimeout = requestTimeout;
        return client;
    }
}
