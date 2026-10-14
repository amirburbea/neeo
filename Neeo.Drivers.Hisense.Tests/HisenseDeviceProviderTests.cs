using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Hisense.Tests;

public sealed class HisenseDeviceProviderTests
{
    [Fact]
    public void DeviceBuilder_configures_expected_capabilities()
    {
        Mock<ISettingsStore<HisenseClientManager>> settingsMock = new();
        HisenseClientManager manager = new(
            NullLogger<HisenseTV>.Instance,
            NullLogger<HisenseClientManager>.Instance,
            settingsMock.Object
        );
        HisenseDeviceProvider provider = new(manager, NullLogger<HisenseDeviceProvider>.Instance);

        IDeviceBuilder builder = provider.DeviceBuilder;

        Assert.NotNull(builder);
        Assert.Equal("IP Controlled TV", builder.Name);
        Assert.Equal("Hisense", builder.Manufacturer);
        Assert.True(builder.HasPowerStateSensor);
        Assert.Contains("Apps", builder.Directories);
    }

    [Fact]
    public void HisenseTV_implements_IDeviceClient_contract()
    {
        IPAddress ip = IPAddress.Parse("192.168.1.50");
        PhysicalAddress mac = PhysicalAddress.Parse("001122334455");
        using HisenseTV tv = new(ip, mac, NullLogger.Instance, useCertificates: false, clientIdPrefix: "test");

        Assert.IsAssignableFrom<IDeviceClient>(tv);
        Assert.Equal(mac.ToString(), tv.DeviceId);
        Assert.False(tv.IsConnected);
        Assert.Equal(0, tv.Volume);
        Assert.Null(tv.CurrentState);
    }

    [Fact]
    public void ServiceConfiguration_registers_HisenseClientManager()
    {
        ServiceConfiguration configuration = new();
        ServiceCollection services = new();
        configuration.ConfigureServices(services);

        ServiceDescriptor? descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(HisenseClientManager));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Synchronous_getters_return_defaults_when_disconnected()
    {
        Mock<ISettingsStore<HisenseClientManager>> settingsMock = new();
        using HisenseClientManager manager = new(
            NullLogger<HisenseTV>.Instance,
            NullLogger<HisenseClientManager>.Instance,
            settingsMock.Object
        );
        HisenseDeviceProvider provider = new(manager, NullLogger<HisenseDeviceProvider>.Instance);

        MethodInfo? getPowerState = typeof(HisenseDeviceProvider).GetMethod("GetPowerState", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo? getVolume = typeof(HisenseDeviceProvider).GetMethod("GetVolume", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo? getState = typeof(HisenseDeviceProvider).GetMethod("GetState", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(getPowerState);
        Assert.NotNull(getVolume);
        Assert.NotNull(getState);

        bool power = (bool)getPowerState.Invoke(provider, ["unknown-device"])!;
        double volume = (double)getVolume.Invoke(provider, ["unknown-device"])!;
        string state = (string)getState.Invoke(provider, ["unknown-device"])!;

        Assert.False(power);
        Assert.Equal(0d, volume);
        Assert.Empty(state);
    }
}
