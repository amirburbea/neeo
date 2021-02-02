using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Remote.Broadlink
{
    public static class RMDiscovery 
    {
        private static readonly Dictionary<string, RMDevice> _remotes = new();

        public static async Task<RMDevice?> DiscoverDeviceAsync(Func<RMDevice, bool>? predicate = default)
        {
            foreach (IPAddress address in await Dns.GetHostAddressesAsync(Dns.GetHostName()).ConfigureAwait(false))
            {
                try
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                    {
                        continue;
                    }
                    using UdpClient client = new(0, AddressFamily.InterNetwork) { EnableBroadcast = true };
                    byte timezone = (byte)(TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes / 3600d);
                    DateTime now = DateTime.Now;
                    byte[] packet = new byte[0x30];
                    if (timezone < 0)
                    {
                        packet[0x08] = (byte)(0xff + timezone - 1);
                        packet[0x09] = 0xff;
                        packet[0x0a] = 0xff;
                        packet[0x0b] = 0xff;
                    }
                    else
                    {
                        packet[0x08] = timezone;
                    }
                    int year = now.Year - 1900;
                    packet[0x0c] = (byte)(year & 0xff);
                    packet[0x0d] = (byte)(year >> 8);
                    packet[0x0e] = (byte)now.Minute;
                    packet[0x0f] = (byte)now.Hour;
                    packet[0x10] = (byte)(year % 100);
                    packet[0x11] = (byte)now.DayOfWeek;
                    packet[0x12] = (byte)now.Day;
                    packet[0x13] = (byte)(now.Month - 1);
                    Buffer.BlockCopy(address.GetAddressBytes(), 0, packet, 0x18, 4);
                    IPEndPoint localEndPoint = (IPEndPoint)client.Client.LocalEndPoint!;
                    packet[0x1c] = (byte)(localEndPoint.Port & 0xff);
                    packet[0x1d] = (byte)(localEndPoint.Port >> 8);
                    packet[0x26] = 6;
                    int checksum = packet.Checksum();
                    packet[0x20] = (byte)(checksum & 0xff);
                    packet[0x21] = (byte)(checksum >> 8);
                    await client.SendAsync(packet, packet.Length, new(IPAddress.Broadcast, 80)).ConfigureAwait(false);
                    Task<UdpReceiveResult> task = client.ReceiveAsync();
                    if (!task.Wait(TimeSpan.FromSeconds(2d)))
                    {
                        continue;
                    }
                    UdpReceiveResult result = task.Result;
                    byte[] mac = new byte[6];
                    for (int index = 0; index < mac.Length; index++)
                    {
                        mac[index] = result.Buffer[0x3f - index];
                    }
                    string macAddress = String.Join(':', mac.Select(b => b.ToString("x2")));
                    if (RMDiscovery._remotes.ContainsKey(macAddress))
                    {
                        continue;
                    }
                    int deviceType = result.Buffer[0x34] | (result.Buffer[0x35] << 8);
                    Debug.WriteLine("Discovered device type 0x{0:x2} with MAC address {1}.", deviceType, macAddress);
                    if (deviceType != 0x6539)
                    {
                        // Not my device type (the protocol needs to be adapted slightly based on the device type).
                        continue;
                    }
                    RMDevice current = new(localEndPoint.Address, result.RemoteEndPoint, mac, deviceType);
                    if (predicate != null && !predicate(current))
                    {
                        continue;
                    }
                    await current.Authenticate().ConfigureAwait(false);
                    TaskCompletionSource source = new();

                    void OnReady(object? sender, EventArgs e)
                    {
                        source.TrySetResult();
                        current.Ready -= OnReady;
                    }

                    current.Ready += OnReady;
                    await source.Task.ConfigureAwait(false);
                    return RMDiscovery._remotes[macAddress] = current;
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
            return default;
        }
    }
}
