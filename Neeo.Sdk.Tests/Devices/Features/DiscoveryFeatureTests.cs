using System;
using System.Linq;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;

public sealed class DiscoveryFeatureTests
{
    [Fact]
    public async Task DiscoverAsync_should_validate_DeviceBuilder_is_null_if_not_EnableDynamicDeviceBuilder()
    {
        DiscoveredDevice deviceWithBuilder = new("id", "", DeviceBuilder: Device.Create("abc", DeviceType.Accessory));
        DiscoveryFeature feature = new((_, _) => Task.FromResult(new[] { deviceWithBuilder }), enableDynamicDeviceBuilder: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => feature.DiscoverAsync());
    }

    [Fact]
    public async Task DiscoverAsync_should_validate_DeviceBuilder_not_null_if_EnableDynamicDeviceBuilder()
    {
        DiscoveredDevice deviceWithoutBuilder = new("id", "");
        DiscoveryFeature feature = new((_, _) => Task.FromResult(new[] { deviceWithoutBuilder }), enableDynamicDeviceBuilder: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => feature.DiscoverAsync());
    }

    [Fact]
    public async Task DiscoverAsync_should_validate_id_not_null_or_empty()
    {
        DiscoveredDevice deviceWithNullId = new(null!, "name");
        DiscoveryFeature feature = new((_, _) => Task.FromResult(new[] { deviceWithNullId }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => feature.DiscoverAsync());
    }

    [Fact]
    public async Task DiscoverAsync_should_validate_ids_are_unique()
    {
        DiscoveredDevice[] devices = Enumerable.Repeat(new DiscoveredDevice("id", ""), 5).ToArray();
        DiscoveryFeature feature = new((_, _) => Task.FromResult(devices));

        await Assert.ThrowsAsync<InvalidOperationException>(() => feature.DiscoverAsync());
    }

    [Fact]
    public async Task DiscoverAsync_should_validate_name_not_null_or_empty()
    {
        DiscoveredDevice deviceWithEmptyName = new("id", "");
        DiscoveryFeature feature = new((_, _) => Task.FromResult(new[] { deviceWithEmptyName }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => feature.DiscoverAsync());
    }

    [Fact]
    public async Task DiscoverAsync_should_validate_optional_device_id()
    {
        DiscoveredDevice device = new("id", "");
        DiscoveryFeature feature = new((_, _) => Task.FromResult(new[] { device }), enableDynamicDeviceBuilder: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => feature.DiscoverAsync("abc"));
    }
}
