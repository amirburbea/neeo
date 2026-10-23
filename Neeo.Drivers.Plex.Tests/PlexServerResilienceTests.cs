using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Neeo.Drivers.Plex.Tests;

public sealed class PlexServerResilienceTests
{
    private const string MachineIdentifier = "machine-1";

    [Fact]
    public async Task TryQueryMediaItem_returns_null_instead_of_throwing_when_the_request_fails()
    {
        using PlexServer server = PlexServerResilienceTests.CreateServer(new((_, _) => throw new HttpRequestException("Plex server unreachable")));

        MediaItem? item = await server.TryQueryMediaItemAsync(1234, TestContext.Current.CancellationToken);

        Assert.Null(item);
    }

    [Fact]
    public async Task TryQueryMediaItem_propagates_cancellation()
    {
        using CancellationTokenSource cts = new();
        using PlexServer server = PlexServerResilienceTests.CreateServer(new((_, token) =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage());
        }));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.TryQueryMediaItemAsync(1234, cts.Token));
    }

    [Fact]
    public async Task TryDiscoverPlayers_does_not_throw_when_the_request_fails()
    {
        using PlexServer server = PlexServerResilienceTests.CreateServer(new((_, _) => throw new HttpRequestException("Plex server unreachable")));

        await server.TryDiscoverPlayersAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Regression test: a failing first poll used to terminate the process through an unhandled Rx error (a crashed test host
    /// fails the run), and left initialization waiting for the poll.
    /// </summary>
    [Fact]
    public async Task Initialization_completes_and_the_process_survives_when_the_server_is_unreachable()
    {
        StubHttpMessageHandler handler = new((_, _) => throw new HttpRequestException("Plex server unreachable"));
        PlexServer server = PlexServerResilienceTests.CreateServer(handler);
        try
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));

            await server.InitializeAsync(timeout.Token);
            for (int i = 0; i < 300 && handler.RequestCount == 0; i++)
            {
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }
            // Give a (formerly) unhandled error time to surface on its thread-pool thread.
            await Task.Delay(500, TestContext.Current.CancellationToken);

            Assert.True(handler.RequestCount >= 1, "The player poll never ran.");
            Assert.False(server.IsConnected);
        }
        finally
        {
            server.Dispose();
        }
    }

    [Fact]
    public async Task Dispose_completes_cleanly_and_signals_Disposed_when_in_flight_tasks_are_cancelled()
    {
        TaskCompletionSource pollStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubHttpMessageHandler handler = new(async (_, token) =>
        {
            pollStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage();
        });
        PlexServer server = PlexServerResilienceTests.CreateServer(handler);
        bool disposedFired = false;
        using IDisposable subscription = server.Disposed.Subscribe(_ => disposedFired = true);

        _ = server.InitializeAsync(TestContext.Current.CancellationToken);
        await pollStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        server.Dispose();

        Assert.True(disposedFired);
    }

    private static PlexServer CreateServer(StubHttpMessageHandler handler) => new(
        PlexServerResilienceTests.MachineIdentifier,
        new HttpClient(handler, disposeHandler: false),
        new FakeSettingsStore(),
        new FakeTokenStore("token"),
        // Nothing listens on port 1, so the notifications socket is refused immediately.
        () => new("Test Server", PlexServerResilienceTests.MachineIdentifier, "127.0.0.1", 1),
        NullLogger<PlexServer>.Instance
    );
}
