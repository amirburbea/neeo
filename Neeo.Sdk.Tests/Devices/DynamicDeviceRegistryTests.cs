using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class DynamicDeviceRegistryTests
{
    private readonly DynamicDeviceRegistry _dynamicDeviceRegistry = new(NullLogger<DynamicDeviceRegistry>.Instance);

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_attempt_discovery_when_not_found()
    {
        var (mockRootAdapter, mockFeature) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);

        await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id", TestContext.Current.CancellationToken);

        mockFeature!.Verify(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_get_previously_registered_device()
    {
        var (mockRootAdapter, _) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);
        Mock<IDeviceAdapter> mockDynamicAdapter = new(MockBehavior.Strict);
        mockDynamicAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockDynamicAdapter));
        mockDynamicAdapter.Setup(adapter => adapter.DeviceCapabilities).Returns([DeviceCapability.DynamicDevice]);
        Mock<IDeviceBuilder> mockDeviceBuilder = new(MockBehavior.Strict);
        mockDeviceBuilder.Setup(device => device.BuildAdapter()).Returns(mockDynamicAdapter.Object);

        this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockRootAdapter.Object, "id", mockDeviceBuilder.Object);
        var discoveredDevice = await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id", TestContext.Current.CancellationToken);

        Assert.Equal(mockDynamicAdapter.Object, discoveredDevice);
    }

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_retry_discovery_after_a_synchronous_miss()
    {
        // ReturnsAsync completes synchronously - a driver returning Task.FromResult(null) must not be cached as "not found".
        var (mockRootAdapter, mockFeature) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);

        Assert.Null(await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id", TestContext.Current.CancellationToken));
        Assert.Null(await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id", TestContext.Current.CancellationToken));

        mockFeature!.Verify(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_not_cancel_other_waiters_when_one_caller_cancels()
    {
        var (mockRootAdapter, mockFeature) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);
        TaskCompletionSource<DiscoveredDevice?> discovery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        mockFeature!.Setup(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>())).Returns(discovery.Task);
        using CancellationTokenSource firstCallerCancellation = new();

        Task<IDeviceAdapter?> first = this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id", firstCallerCancellation.Token).AsTask();
        Task<IDeviceAdapter?> second = this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id", TestContext.Current.CancellationToken).AsTask();
        firstCallerCancellation.Cancel();
        discovery.SetResult(null);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Null(await second);
        mockFeature.Verify(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void RegisterDiscoveredDevice_should_throw_if_adapter_does_not_support_discovery()
    {
        var (mockAdapter, _) = CreateRootAdapter(discovery: false);

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockAdapter.Object, "id", Mock.Of<IDeviceBuilder>()));
    }

    [Fact]
    public void RegisterDiscoveredDevice_should_throw_if_adapter_supports_discovery_but_not_EnableDynamicDeviceBuilder()
    {
        var (mockAdapter, _) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: false);

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockAdapter.Object, "id", Mock.Of<IDeviceBuilder>()));
    }

    [Fact]
    public void RegisterDiscoveredDevice_should_throw_if_not_DeviceCapability_DynamicDevice()
    {
        var (mockRootAdapter, _) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);
        Mock<IDeviceAdapter> mockDynamicAdapter = new(MockBehavior.Strict);
        mockDynamicAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockDynamicAdapter));
        mockDynamicAdapter.Setup(adapter => adapter.DeviceCapabilities).Returns([]);
        Mock<IDeviceBuilder> mockDeviceBuilder = new(MockBehavior.Strict);
        mockDeviceBuilder.Setup(device => device.BuildAdapter()).Returns(mockDynamicAdapter.Object);

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockRootAdapter.Object, "id", mockDeviceBuilder.Object));
    }

    private static (Mock<IDeviceAdapter>, Mock<IDiscoveryFeature>?) CreateRootAdapter(bool discovery = false, bool enableDynamicDeviceBuilder = false)
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockAdapter));
        Mock<IDiscoveryFeature>? mockFeature;
        if (discovery)
        {
            mockFeature = new(MockBehavior.Strict);
            mockFeature.Setup(feature => feature.EnableDynamicDeviceBuilder).Returns(enableDynamicDeviceBuilder);
            mockFeature.Setup(feature => feature.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
            mockFeature.Setup(feature => feature.DiscoverAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(value: null);
        }
        else
        {
            mockFeature = null;
        }
        mockAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(mockFeature?.Object);
        return (mockAdapter, mockFeature);
    }
}
