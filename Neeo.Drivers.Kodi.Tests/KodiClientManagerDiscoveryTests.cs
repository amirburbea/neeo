using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Neeo.Drivers.Kodi;
using Neeo.Sdk.Utilities;
using Xunit;
using Zeroconf;

namespace Neeo.Drivers.Kodi.Tests;

public sealed class KodiClientManagerDiscoveryTests
{
    private const string MacAddress = "AA:BB:CC:DD:EE:01";

    [Fact]
    public async Task DiscoverAsync_waits_for_a_host_still_connecting_when_the_scan_ends()
    {
        TaskCompletionSource macRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseMac = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using FakeKodiServer server = await FakeKodiServer.StartAsync(async (socket, request, _) =>
        {
            macRequested.TrySetResult();
            await releaseMac.Task;
            await FakeKodiServer.RespondAsync(socket, request, $$"""{"Network.MacAddress":"{{KodiClientManagerDiscoveryTests.MacAddress}}"}""");
        });
        RecordingLogger<KodiClient> logger = new();
        using KodiClientManager manager = KodiClientManagerDiscoveryTests.CreateManager(
            server.Port,
            logger,
            KodiClientManagerDiscoveryTests.ScannerThatFinds(KodiClientManagerDiscoveryTests.CreateHost())
        );

        Task discovery = manager.DiscoverAsync(cancellationToken: TestContext.Current.CancellationToken);
        await macRequested.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // The scan itself is over, but the host found by it is still being connected to.
        Assert.False(discovery.IsCompleted);
        releaseMac.SetResult();
        await discovery.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Single(manager.Clients);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task DiscoverAsync_returns_quietly_once_considerDiscoveryComplete_is_satisfied()
    {
        await using FakeKodiServer server = await KodiClientManagerDiscoveryTests.StartServerAsync();
        RecordingLogger<KodiClient> logger = new();
        bool cancelled = false;
        // Like the real scanner, keeps going until cancelled, then ends with a cancellation.
        Mock<IKodiHostLocator> scanner = new();
        scanner
            .Setup(s => s.ScanAsync(It.IsAny<TimeSpan>(), It.IsAny<Action<IZeroconfHost>>(), It.IsAny<CancellationToken>()))
            .Returns(async (TimeSpan _, Action<IZeroconfHost> onHost, CancellationToken token) =>
            {
                onHost(KodiClientManagerDiscoveryTests.CreateHost());
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    throw;
                }
            });
        using KodiClientManager manager = KodiClientManagerDiscoveryTests.CreateManager(server.Port, logger, scanner.Object);

        await manager.DiscoverAsync(5000, static _ => true, TestContext.Current.CancellationToken);

        Assert.True(cancelled);
        Assert.Single(manager.Clients);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task TryAddHostAsync_allows_retry_when_previous_connection_attempt_cancelled_synchronously()
    {
        await using FakeKodiServer server = await KodiClientManagerDiscoveryTests.StartServerAsync();
        using KodiClientManager manager = KodiClientManagerDiscoveryTests.CreateManager(server.Port, new RecordingLogger<KodiClient>(), Mock.Of<IKodiHostLocator>());

        using CancellationTokenSource cts = new();
        cts.Cancel();
        Assert.False(await manager.TryAddHostAsync("127.0.0.1", server.Port, cts.Token));

        Assert.True(await manager.TryAddHostAsync("127.0.0.1", server.Port, TestContext.Current.CancellationToken));
        Assert.Single(manager.Clients);
    }

    [Fact]
    public async Task DiscoverAsync_throws_when_the_caller_cancels()
    {
        Mock<IKodiHostLocator> scanner = new();
        scanner
            .Setup(s => s.ScanAsync(It.IsAny<TimeSpan>(), It.IsAny<Action<IZeroconfHost>>(), It.IsAny<CancellationToken>()))
            .Returns((TimeSpan _, Action<IZeroconfHost> _, CancellationToken token) => Task.Delay(Timeout.Infinite, token));
        using KodiClientManager manager = KodiClientManagerDiscoveryTests.CreateManager(1, new RecordingLogger<KodiClient>(), scanner.Object);
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        Task discovery = manager.DiscoverAsync(cancellationToken: cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => discovery);
    }

    [Fact]
    public async Task DiscoverAsync_connects_to_every_host_found_even_if_one_fails()
    {
        await using FakeKodiServer server = await KodiClientManagerDiscoveryTests.StartServerAsync();
        RecordingLogger<KodiClient> logger = new();
        Mock<IZeroconfHost> broken = new();
        broken.Setup(h => h.DisplayName).Returns("Broken");
        broken.Setup(h => h.IPAddress).Throws(new InvalidOperationException("Bad record."));
        using KodiClientManager manager = KodiClientManagerDiscoveryTests.CreateManager(
            server.Port,
            logger,
            KodiClientManagerDiscoveryTests.ScannerThatFinds(broken.Object, KodiClientManagerDiscoveryTests.CreateHost())
        );

        await manager.DiscoverAsync(5000, static _ => false, TestContext.Current.CancellationToken);

        Assert.Single(manager.Clients);
        Assert.Single(logger.Entries, static entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Broken"));
    }

    [Fact]
    public async Task DiscoverAsync_skips_a_host_without_a_usable_address()
    {
        RecordingLogger<KodiClient> logger = new();
        Mock<IZeroconfHost> host = new();
        host.Setup(h => h.DisplayName).Returns("Broken");
        host.Setup(h => h.IPAddress).Returns(string.Empty);
        host.Setup(h => h.IPAddresses).Returns([]);
        using KodiClientManager manager = KodiClientManagerDiscoveryTests.CreateManager(1, logger, KodiClientManagerDiscoveryTests.ScannerThatFinds(host.Object));

        await manager.DiscoverAsync(5000, static _ => true, TestContext.Current.CancellationToken);

        Assert.Empty(manager.Clients);
    }

    private static Task<FakeKodiServer> StartServerAsync() => FakeKodiServer.StartAsync(
        (socket, request, _) => FakeKodiServer.RespondAsync(socket, request, $$"""{"Network.MacAddress":"{{KodiClientManagerDiscoveryTests.MacAddress}}"}""")
    );

    private static KodiClientManager CreateManager(int webSocketPort, ILogger<KodiClient> logger, IKodiHostLocator scanner) => new(
        logger,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Kodi:WebSocketPort"] = webSocketPort.ToString() }).Build(),
        Mock.Of<ISettingsStore<KodiClientManager>>(),
        Mock.Of<System.Net.Http.IHttpClientFactory>(),
        scanner
    );

    private static IKodiHostLocator ScannerThatFinds(params IZeroconfHost[] hosts)
    {
        Mock<IKodiHostLocator> scanner = new();
        scanner
            .Setup(s => s.ScanAsync(It.IsAny<TimeSpan>(), It.IsAny<Action<IZeroconfHost>>(), It.IsAny<CancellationToken>()))
            .Returns((TimeSpan _, Action<IZeroconfHost> onHost, CancellationToken _) =>
            {
                foreach (IZeroconfHost host in hosts)
                {
                    onHost(host);
                }
                return Task.CompletedTask;
            });
        return scanner.Object;
    }

    private static IZeroconfHost CreateHost(string ipAddress = "127.0.0.1")
    {
        Mock<IService> service = new();
        service.Setup(s => s.Port).Returns(8080);
        Mock<IZeroconfHost> host = new();
        host.Setup(h => h.DisplayName).Returns($"Kodi {ipAddress}");
        host.Setup(h => h.IPAddress).Returns(ipAddress);
        host.Setup(h => h.IPAddresses).Returns([ipAddress]);
        host.Setup(h => h.Services).Returns(new Dictionary<string, IService> { [ZeroconfKodiHostLocator.HttpServiceName] = service.Object });
        return host.Object;
    }
}
