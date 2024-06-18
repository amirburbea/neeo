using System;
using System.Buffers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
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
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task WakeAsync(string macAddress, CancellationToken cancellationToken = default) => PhysicalAddress.Parse(macAddress).WakeAsync(cancellationToken);

    /// <summary>
    /// Sends the Wake-On-Lan magic packet to a network card with the specified MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address associated with the the network card of the device to wake.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task WakeAsync(this PhysicalAddress macAddress, CancellationToken cancellationToken = default)
    {
        if (macAddress.GetAddressBytes() is not { Length: 6 } addressBytes)
        {
            throw new ArgumentException("Invalid MAC address.", nameof(macAddress));
        }
        using IMemoryOwner<byte> magicPacket = MemoryPool<byte>.Shared.Rent(Constants.Length);
        magicPacket.Memory.Span[0..6].Fill(byte.MaxValue);
        for (int index = 1; index <= 16; index++)
        {
            addressBytes.CopyTo(magicPacket.Memory[(index * 6)..]);
        }
        using UdpClient client = new();
        await client.SendAsync(magicPacket.Memory[..Constants.Length], new(IPAddress.Broadcast, 9), cancellationToken).ConfigureAwait(false);
    }

    private static class Constants
    {
        public const int Length = 6 + 16 * 6;
    }
}
