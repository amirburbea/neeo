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
        Mock<IDeviceAdapter> mockRootAdapter = new(MockBehavior.Strict);
        mockRootAdapter.Setup(adapter => adapter.AdapterName).Returns("adapter");
        Mock<IDiscoveryFeature> mockFeature = new(MockBehavior.Strict);
        mockFeature.Setup(feature => feature.EnableDynamicDeviceBuilder).Returns(true);
        mockFeature.Setup(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DiscoveredDevice>());
        mockRootAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(mockFeature.Object);

        await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id");

        mockFeature.Verify(feature => feature.DiscoverAsync("id", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetDiscoveredDeviceAsync_should_get_previously_registered_device()
    {
        Mock<IDeviceAdapter> mockRootAdapter = new(MockBehavior.Strict);
        mockRootAdapter.Setup(adapter => adapter.AdapterName).Returns("adapter");
        Mock<IDiscoveryFeature> mockFeature = new(MockBehavior.Strict);
        mockFeature.Setup(feature => feature.EnableDynamicDeviceBuilder).Returns(true);
        mockRootAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(mockFeature.Object);
        Mock<IDeviceAdapter> mockDynamicAdapter = new(MockBehavior.Strict);
        mockDynamicAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockDynamicAdapter));
        mockDynamicAdapter.Setup(adapter => adapter.DeviceCapabilities).Returns(new[] { DeviceCapability.DynamicDevice });
        Mock<IDeviceBuilder> mockDeviceBuilder = new(MockBehavior.Strict);
        mockDeviceBuilder.Setup(device => device.BuildAdapter()).Returns(mockDynamicAdapter.Object);

        this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockRootAdapter.Object, "id", mockDeviceBuilder.Object);
        var discoveredDevice = await this._dynamicDeviceRegistry.GetDiscoveredDeviceAsync(mockRootAdapter.Object, "id");

        Assert.Equal(mockDynamicAdapter.Object, discoveredDevice);
    }

    [Fact]
    public void RegisterDiscoveredDevice_should_throw_if_adapter_does_not_support_discovery()
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(value: null);

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockAdapter.Object, "id", Mock.Of<IDeviceBuilder>()));
    }

    [Fact]
    public void RegisterDiscoveredDevice_should_throw_if_adapter_supports_discovery_but_not_EnableDynamicDeviceBuilder()
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        Mock<IDiscoveryFeature> mockFeature = new(MockBehavior.Strict);
        mockFeature.Setup(feature => feature.EnableDynamicDeviceBuilder).Returns(false);
        mockAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(mockFeature.Object);

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockAdapter.Object, "id", Mock.Of<IDeviceBuilder>()));
    }

    [Fact]
    public void RegisterDiscoveredDevice_should_throw_if_not_DeviceCapability_DynamicDevice()
    {
        Mock<IDeviceAdapter> mockRootAdapter = new(MockBehavior.Strict);
        mockRootAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockRootAdapter));
        Mock<IDiscoveryFeature> mockFeature = new(MockBehavior.Strict);
        mockFeature.Setup(feature => feature.EnableDynamicDeviceBuilder).Returns(true);
        mockRootAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Discovery)).Returns(mockFeature.Object);
        Mock<IDeviceAdapter> mockDynamicAdapter = new(MockBehavior.Strict);
        mockDynamicAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockDynamicAdapter));
        mockDynamicAdapter.Setup(adapter => adapter.DeviceCapabilities).Returns(Array.Empty<DeviceCapability>());
        Mock<IDeviceBuilder> mockDeviceBuilder = new(MockBehavior.Strict);
        mockDeviceBuilder.Setup(device => device.BuildAdapter()).Returns(mockDynamicAdapter.Object);

        Assert.Throws<ArgumentException>(() => this._dynamicDeviceRegistry.RegisterDiscoveredDevice(mockRootAdapter.Object, "id", mockDeviceBuilder.Object));
    }
}