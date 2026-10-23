using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class NetworkMethodsTests
{
    private const string Header = "IP address       HW type     Flags       HW address            Mask     Device";

    [Fact]
    public void ParseProcNetArp_adds_complete_entries()
    {
        Dictionary<IPAddress, PhysicalAddress> output = [];

        NetworkMethods.ParseProcNetArp(
            [
                Header,
                "192.168.1.1      0x1         0x2         ea:a2:5c:ed:d3:cb     *        eth0",
                "192.168.1.20     0x1         0x6         00:11:22:33:44:55     *        eth0",
            ],
            output
        );

        Assert.Equal(2, output.Count);
        Assert.Equal(PhysicalAddress.Parse("EA-A2-5C-ED-D3-CB"), output[IPAddress.Parse("192.168.1.1")]);
        Assert.Equal(PhysicalAddress.Parse("00-11-22-33-44-55"), output[IPAddress.Parse("192.168.1.20")]);
    }

    [Fact]
    public void ParseProcNetArp_skips_incomplete_entries()
    {
        Dictionary<IPAddress, PhysicalAddress> output = [];

        NetworkMethods.ParseProcNetArp(
            [
                Header,
                "192.168.1.77     0x1         0x0         00:00:00:00:00:00     *        eth0",
            ],
            output
        );

        Assert.Empty(output);
    }

    [Fact]
    public void ParseProcNetArp_ignores_header_and_malformed_lines()
    {
        Dictionary<IPAddress, PhysicalAddress> output = [];

        NetworkMethods.ParseProcNetArp([Header, string.Empty, "not an entry", "192.168.1.1 0x1"], output);

        Assert.Empty(output);
    }

    [Fact]
    public void ParseProcNetArp_keeps_the_last_entry_for_an_address_listed_on_two_interfaces()
    {
        Dictionary<IPAddress, PhysicalAddress> output = [];

        NetworkMethods.ParseProcNetArp(
            [
                "192.168.1.1      0x1         0x2         aa:aa:aa:aa:aa:aa     *        eth0",
                "192.168.1.1      0x1         0x2         bb:bb:bb:bb:bb:bb     *        wlan0",
            ],
            output
        );

        Assert.Equal(PhysicalAddress.Parse("BB-BB-BB-BB-BB-BB"), Assert.Single(output).Value);
    }
}
