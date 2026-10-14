using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Hisense.Tests;

public sealed class HisenseClientManagerTests
{
    private static readonly PhysicalAddress _testMac = PhysicalAddress.Parse("00-11-22-33-44-55");
    private static readonly IPAddress _testIp = IPAddress.Parse("192.168.1.50");

    [Fact]
    public void GetOrCreateClient_adds_client_to_registry_and_saves_ip()
    {
        Dictionary<string, string> store = [];
        Mock<ISettingsStore<HisenseClientManager>> settingsMock = new();
        settingsMock.Setup(s => s[It.IsAny<string>()]).Returns<string>(k => store.GetValueOrDefault(k)!);
        settingsMock.SetupSet(s => s[It.IsAny<string>()] = It.IsAny<string>()).Callback<string, string>((k, v) => store[k] = v);

        using HisenseClientManager manager = new(
            NullLogger<HisenseTV>.Instance,
            NullLogger<HisenseClientManager>.Instance,
            settingsMock.Object
        );

        HisenseTV tv = manager.GetOrCreateClient(_testIp, _testMac);

        Assert.NotNull(tv);
        Assert.Equal(_testMac.ToString(), tv.DeviceId);
        Assert.Equal(_testIp, tv.IPAddress);
        Assert.Same(tv, manager.GetClient(tv.DeviceId));
        Assert.Single(manager.Clients);
        Assert.Equal("192.168.1.50", store[$"{_testMac}|ip"]);

        // Calling again returns the existing client
        HisenseTV sameTv = manager.GetOrCreateClient(_testIp, _testMac);
        Assert.Same(tv, sameTv);
    }

    [Fact]
    public void GetOrCreateKnownClient_uses_stored_ip_when_not_in_arp_table()
    {
        Dictionary<string, string> store = new()
        {
            [$"{_testMac}|ip"] = "192.168.1.75"
        };
        Mock<ISettingsStore<HisenseClientManager>> settingsMock = new();
        settingsMock.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns<string, string?>((k, def) => store.TryGetValue(k, out string? v) ? v : def);

        using HisenseClientManager manager = new(
            NullLogger<HisenseTV>.Instance,
            NullLogger<HisenseClientManager>.Instance,
            settingsMock.Object
        );

        HisenseTV? tv = manager.GetOrCreateKnownClient(_testMac.ToString());

        Assert.NotNull(tv);
        Assert.Equal(_testMac.ToString(), tv.DeviceId);
        Assert.Equal(IPAddress.Parse("192.168.1.75"), tv.IPAddress);
    }

    [Fact]
    public void GetOrCreateKnownClient_returns_null_for_invalid_mac()
    {
        Mock<ISettingsStore<HisenseClientManager>> settingsMock = new();
        using HisenseClientManager manager = new(
            NullLogger<HisenseTV>.Instance,
            NullLogger<HisenseClientManager>.Instance,
            settingsMock.Object
        );

        Assert.Null(manager.GetOrCreateKnownClient("invalid-mac-address"));
    }
}
