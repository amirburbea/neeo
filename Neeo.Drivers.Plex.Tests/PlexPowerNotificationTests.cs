using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Plex.Tests;

public sealed class PlexPowerNotificationTests
{
    [Fact]
    public async Task A_server_added_by_the_brain_sends_power_notifications_as_it_connects_and_disconnects()
    {
        const string MachineIdentifier = "server-1";
        (Mock<IPlexServer> server, BehaviorSubject<bool> isConnected) = PlexPowerNotificationTests.CreateServer(MachineIdentifier, isConnected: false);
        Mock<IPlexServerManager> manager = new();
        manager.Setup(m => m.GetServer(MachineIdentifier)).Returns(server.Object);
        Mock<IDeviceNotifier> notifier = new();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);
        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);

        isConnected.OnNext(true);
        isConnected.OnNext(false);

        notifier.Verify(n => n.SendPowerNotificationAsync(true, MachineIdentifier, It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.SendPowerNotificationAsync(false, MachineIdentifier, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_server_that_is_already_connected_when_added_sends_a_power_notification()
    {
        const string MachineIdentifier = "server-1";
        (Mock<IPlexServer> server, _) = PlexPowerNotificationTests.CreateServer(MachineIdentifier, isConnected: true);
        Mock<IPlexServerManager> manager = new();
        manager.Setup(m => m.GetServer(MachineIdentifier)).Returns(server.Object);
        Mock<IDeviceNotifier> notifier = new();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);

        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);

        notifier.Verify(n => n.SendPowerNotificationAsync(true, MachineIdentifier, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_server_not_found_when_added_by_the_brain_attaches_when_later_discovered()
    {
        const string MachineIdentifier = "server-1";
        (Mock<IPlexServer> server, _) = PlexPowerNotificationTests.CreateServer(MachineIdentifier, isConnected: true);
        Mock<IPlexServerManager> manager = new();
        manager.SetupSequence(m => m.GetServer(MachineIdentifier))
            .Returns((IPlexServer?)null)
            .Returns(server.Object);
        Mock<IDeviceNotifier> notifier = new();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);

        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);
        notifier.Verify(n => n.SendPowerNotificationAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        manager.Raise(m => m.ServerDiscovered += null, new DataEventArgs<ServerData>(new ServerData("Plex", MachineIdentifier, "127.0.0.1", 32400)));

        notifier.Verify(n => n.SendPowerNotificationAsync(true, MachineIdentifier, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (Mock<IPlexServer> Server, BehaviorSubject<bool> IsConnected) CreateServer(string machineIdentifier, bool isConnected)
    {
        BehaviorSubject<bool> isConnectedSubject = new(isConnected);
        Mock<IPlexServer> server = new();
        server.SetupGet(s => s.MachineIdentifier).Returns(machineIdentifier);
        server.SetupGet(s => s.IsConnected).Returns(() => isConnectedSubject.Value);
        server.SetupGet(s => s.IsConnectedChanged).Returns(isConnectedSubject);
        server.SetupGet(s => s.Disposed).Returns(new Subject<Unit>());
        server.SetupGet(s => s.SelectedPlayerChanged).Returns(new BehaviorSubject<PlayerData?>(null));
        server.SetupGet(s => s.PlayStateChanged).Returns(new BehaviorSubject<PlayState>(PlayState.Stopped));
        server.SetupGet(s => s.ActiveMediaChanged).Returns(new BehaviorSubject<MediaItem?>(null));
        return (server, isConnectedSubject);
    }

    private static PlexMediaPlayerDeviceProvider CreateProvider(IPlexServerManager manager)
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient());
        Mock<IHostApplicationLifetime> lifetime = new();
        lifetime.SetupGet(l => l.ApplicationStarted).Returns(new CancellationToken(true));
        return new(factory.Object, manager, Mock.Of<IPlexTokenStore>(), lifetime.Object, NullLogger<PlexMediaPlayerDeviceProvider>.Instance);
    }
}
