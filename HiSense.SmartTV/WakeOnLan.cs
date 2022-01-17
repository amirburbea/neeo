using System.Buffers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HiSense.SmartTV;

public static class WakeOnLan
{
    public static Task WakeAsync(string macAddress) => PhysicalAddress.Parse(macAddress).WakeAsync();

    public static async Task WakeAsync(this PhysicalAddress address)
    {
        const int length = 102;
        byte[] targetBytes = address.GetAddressBytes();
        byte[] magicPacket = ArrayPool<byte>.Shared.Rent(length);
        magicPacket.AsSpan(0, 6).Fill(byte.MaxValue);
        for (int i = 0; i < 16; i++)
        {
            targetBytes.CopyTo(magicPacket.AsSpan(6 + (i * 6)));
        }
        using UdpClient client = new();
        await client.SendAsync(magicPacket, length, new(IPAddress.Broadcast, 9)).ConfigureAwait(false);
        ArrayPool<byte>.Shared.Return(magicPacket);
    }
}