using System;
using System.Collections.Generic;
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
    private const string MachineIdentifier = "server-1";

    [Fact]
    public async Task A_server_added_by_the_brain_reports_its_power_state_as_it_connects_and_disconnects()
    {
        TestServer server = PlexPowerNotificationTests.CreateServer(isConnected: false);
        Mock<IPlexServerManager> manager = new();
        manager.Setup(m => m.GetServer(MachineIdentifier)).Returns(server.Mock.Object);
        (Mock<IDeviceNotifier> notifier, List<bool> powerStates) = PlexPowerNotificationTests.CreateNotifier();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);
        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);

        server.IsConnected.OnNext(true);
        server.IsConnected.OnNext(false);

        Assert.Equal([false, true, false], powerStates);
    }

    [Fact]
    public async Task A_server_that_is_already_connected_when_added_reports_that_it_is_on()
    {
        TestServer server = PlexPowerNotificationTests.CreateServer(isConnected: true);
        Mock<IPlexServerManager> manager = new();
        manager.Setup(m => m.GetServer(MachineIdentifier)).Returns(server.Mock.Object);
        (Mock<IDeviceNotifier> notifier, List<bool> powerStates) = PlexPowerNotificationTests.CreateNotifier();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);

        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);

        Assert.Equal([true], powerStates);
    }

    [Fact]
    public async Task A_server_not_found_when_added_by_the_brain_attaches_when_later_discovered()
    {
        TestServer server = PlexPowerNotificationTests.CreateServer(isConnected: true);
        Mock<IPlexServerManager> manager = new();
        manager.SetupSequence(m => m.GetServer(MachineIdentifier))
            .Returns((IPlexServer?)null)
            .Returns(server.Mock.Object);
        (Mock<IDeviceNotifier> notifier, List<bool> powerStates) = PlexPowerNotificationTests.CreateNotifier();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);

        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);
        Assert.Empty(powerStates);

        manager.Raise(m => m.ServerDiscovered += null, new DataEventArgs<ServerData>(new ServerData("Plex", MachineIdentifier, "127.0.0.1", 32400)));

        Assert.Equal([true], powerStates);
        notifier.Verify(n => n.NotifyPowerStateOnChange(It.IsAny<IObservable<bool>>(), MachineIdentifier), Times.Once);
    }

    [Fact]
    public async Task Power_state_is_no_longer_reported_once_the_server_is_disposed()
    {
        TestServer server = PlexPowerNotificationTests.CreateServer(isConnected: false);
        Mock<IPlexServerManager> manager = new();
        manager.Setup(m => m.GetServer(MachineIdentifier)).Returns(server.Mock.Object);
        (Mock<IDeviceNotifier> notifier, List<bool> powerStates) = PlexPowerNotificationTests.CreateNotifier();
        PlexMediaPlayerDeviceProvider provider = PlexPowerNotificationTests.CreateProvider(manager.Object);
        provider.DeviceBuilder.NotifierCallback!(notifier.Object);
        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceAddedAsync(MachineIdentifier, TestContext.Current.CancellationToken);

        server.Disposed.OnNext(Unit.Default);
        server.IsConnected.OnNext(true);

        Assert.Equal([false], powerStates);
    }

    private static (Mock<IDeviceNotifier> Notifier, List<bool> PowerStates) CreateNotifier()
    {
        List<bool> powerStates = [];
        Mock<IDeviceNotifier> notifier = new();
        notifier
            .Setup(n => n.NotifyPowerStateOnChange(It.IsAny<IObservable<bool>>(), It.IsAny<string>()))
            .Returns((IObservable<bool> values, string _) => values.Subscribe(powerStates.Add));
        return (notifier, powerStates);
    }

    private static TestServer CreateServer(bool isConnected)
    {
        BehaviorSubject<bool> isConnectedSubject = new(isConnected);
        Subject<Unit> disposed = new();
        Mock<IPlexServer> server = new();
        server.SetupGet(s => s.MachineIdentifier).Returns(MachineIdentifier);
        server.SetupGet(s => s.IsConnected).Returns(() => isConnectedSubject.Value);
        server.SetupGet(s => s.IsConnectedChanged).Returns(isConnectedSubject);
        server.SetupGet(s => s.Disposed).Returns(disposed);
        server.SetupGet(s => s.SelectedPlayerChanged).Returns(new BehaviorSubject<PlayerData?>(null));
        server.SetupGet(s => s.PlayStateChanged).Returns(new BehaviorSubject<PlayState>(PlayState.Stopped));
        server.SetupGet(s => s.ActiveMediaChanged).Returns(new BehaviorSubject<MediaItem?>(null));
        return new(server, isConnectedSubject, disposed);
    }

    private static PlexMediaPlayerDeviceProvider CreateProvider(IPlexServerManager manager)
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient());
        Mock<IHostApplicationLifetime> lifetime = new();
        lifetime.SetupGet(l => l.ApplicationStarted).Returns(new CancellationToken(true));
        return new(factory.Object, manager, Mock.Of<IPlexTokenStore>(), lifetime.Object, NullLogger<PlexMediaPlayerDeviceProvider>.Instance);
    }

    private sealed record TestServer(Mock<IPlexServer> Mock, BehaviorSubject<bool> IsConnected, Subject<Unit> Disposed);
}
