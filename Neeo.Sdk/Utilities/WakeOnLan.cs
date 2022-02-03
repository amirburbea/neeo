using System;
using System.Buffers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// A utility for sending Wake-On-Lan notifications.
/// While not required for interacting with the NEEO Brain, it could be useful in a number of device drivers.
/// </summary>
public static class WakeOnLan
{
    /// <summary>
    /// Sends the Wake-On-Lan magic packet to a network card with the specified MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address associated with the the network card of the device to wake.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task WakeAsync(string macAddress) => PhysicalAddress.Parse(macAddress).WakeAsync();

    /// <summary>
    /// Sends the Wake-On-Lan magic packet to a network card with the specified MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address associated with the the network card of the device to wake.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task WakeAsync(this PhysicalAddress macAddress)
    {
        const int length = 102;
        byte[] magicPacket = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            byte[] addressBytes = macAddress.GetAddressBytes();
            magicPacket.AsSpan(0, 6).Fill(byte.MaxValue);
            for (int i = 0; i < 16; i++)
            {
                addressBytes.CopyTo(magicPacket.AsSpan(6 + (i * 6)));
            }
            using UdpClient client = new();
            await client.SendAsync(magicPacket, length, new(IPAddress.Broadcast, 9)).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(magicPacket);
        }
    }
}