using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class DynamicDeviceRegistryTests
{
    private readonly DynamicDeviceRegistry _dynamicDeviceRegistry;
    private readonly Mock<IDeviceFactory> _mockFactory;

    public DynamicDeviceRegistryTests()
    {
        this._mockFactory = new(MockBehavior.Strict);
        this._dynamicDeviceRegistry = new(this._mockFactory.Object, NullLogger<DynamicDeviceRegistry>.Instance);
    }

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_attempt_discovery_when_not_found()
    {
        var (mockRootAdapter, mockFeature) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);

        await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id");

        mockFeature!.Verify(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_get_previously_registered_device()
    {
        var (mockRootAdapter, _) = CreateRootAdapter(discovery: true, enableDynamicDeviceBuilder: true);
        var (dynamicAdapter, deviceBuilder) = this.SetUpAdapter(DeviceCapability.DynamicDevice);

        this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockRootAdapter.Object, "id", deviceBuilder);
        var discoveredDevice = await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id");

        Assert.Equal(dynamicAdapter, discoveredDevice);
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
        var (dynamicAdapter, deviceBuilder) = this.SetUpAdapter();

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockRootAdapter.Object, "id", deviceBuilder));
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
            mockFeature.Setup(feature => feature.DiscoverAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DiscoveredDevice>());
        }
        else
        {
            mockFeature = null;
        }
        mockAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(mockFeature?.Object);
        return (mockAdapter, mockFeature);
    }

    private (IDeviceAdapter, IDeviceBuilder) SetUpAdapter(params DeviceCapability[] deviceCapabilities)
    {
        Mock<IDeviceAdapter> mockDynamicAdapter = new(MockBehavior.Strict);
        mockDynamicAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockDynamicAdapter));
        mockDynamicAdapter.Setup(adapter => adapter.DeviceCapabilities).Returns(deviceCapabilities);
        Mock<IDeviceBuilder> mockDeviceBuilder = new(MockBehavior.Strict);
        this._mockFactory.Setup(factory => factory.BuildDevice(mockDeviceBuilder.Object)).Returns(mockDynamicAdapter.Object);
        return (mockDynamicAdapter.Object, mockDeviceBuilder.Object);
    }
}