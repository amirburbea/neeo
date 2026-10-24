using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Xunit;

namespace Neeo.Drivers.Plex.Tests;

public sealed class PlexPowerNotificationTests
{
    [Fact]
    public async Task A_server_added_by_the_brain_sends_power_notifications_as_it_connects_and_disconnects()
    {
        const string MachineIdentifier = "server-1";
        BehaviorSubject<bool> isConnected = new(false);
        Subject<Unit> disposed = new();
        Mock<IPlexServer> server = new();
        server.SetupGet(s => s.MachineIdentifier).Returns(MachineIdentifier);
        server.SetupGet(s => s.IsConnected).Returns(() => isConnected.Value);
        server.SetupGet(s => s.IsConnectedChanged).Returns(isConnected);
        server.SetupGet(s => s.Disposed).Returns(disposed);
        server.SetupGet(s => s.SelectedPlayerChanged).Returns(new BehaviorSubject<PlayerData?>(null));
        server.SetupGet(s => s.PlayStateChanged).Returns(new BehaviorSubject<PlayState>(PlayState.Stopped));
        server.SetupGet(s => s.ActiveMediaChanged).Returns(new BehaviorSubject<MediaItem?>(null));
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

    private static PlexMediaPlayerDeviceProvider CreateProvider(IPlexServerManager manager)
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient());
        Mock<IHostApplicationLifetime> lifetime = new();
        lifetime.SetupGet(l => l.ApplicationStarted).Returns(new CancellationToken(true));
        return new(factory.Object, manager, Mock.Of<IPlexTokenStore>(), lifetime.Object, NullLogger<PlexMediaPlayerDeviceProvider>.Instance);
    }
}
