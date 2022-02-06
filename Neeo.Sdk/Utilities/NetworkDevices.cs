﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// A utility class for getting the IP and MAC address of devices on the network.
/// </summary>
public static partial class NetworkDevices
{
    /// <summary>
    /// Gets the devices on the network via a call to the operating system's ARP command.
    /// Note that not all devices may be listed as the ARP table requires the PC to have previously made some contact
    /// (although this is usally the case, as many network devices are &quot;chatty&quot;).
    /// </summary>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public static IReadOnlyDictionary<IPAddress, PhysicalAddress> GetNetworkDevices()
    {
        Dictionary<IPAddress, PhysicalAddress> output = (
            // Seed with information about the local machine.
            from networkInterface in NetworkInterface.GetAllNetworkInterfaces()
            where networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.Wireless80211
            from unicastInfo in networkInterface.GetIPProperties().UnicastAddresses
            where unicastInfo.Address.AddressFamily == AddressFamily.InterNetwork
            select (unicastInfo.Address, PhysicalAddress: networkInterface.GetPhysicalAddress())
        ).ToDictionary(tuple => tuple.Address, tuple => tuple.PhysicalAddress);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Uses a win32 API to get access to the table.
            NetworkDevices.PopulateNetworkDevicesViaInterop(output);
        }
        else
        {
            // Runs the *nix command and parses the output.
            NetworkDevices.PopulateNetworkDevicesViaCommandLine(output);
        }
        return output;
    }
}