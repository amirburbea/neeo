using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Remote.Neeo.Devices;
using Remote.Neeo.Web;

namespace Remote.Neeo
{
    public static class NeeoSdk
    {
        public static IDeviceBuilder CreateDevice(string name, DeviceType deviceType) => new DeviceBuilder(name) { Type = deviceType };

        public static Task StartServerAsync(Brain brain, string name, params IDeviceBuilder[] devices) => NeeoSdk.StartServerAsync(brain, name, devices, cancellationToken: default);

        public static Task StartServerAsync(Brain brain, string name, IDeviceBuilder[] devices, string? hostIPAddress = default, ushort port = 8080, CancellationToken cancellationToken = default)
        {
            return Server.StartAsync(
                brain,
                name,
                devices,
                hostIPAddress is null ? GetHostIPAddress(brain.IPAddress) : IPAddress.Parse(hostIPAddress),
                port,
                cancellationToken
            );
        }

        public static Task StopServerAsync(CancellationToken cancellationToken = default) => Server.StopAsync(cancellationToken);

        private static IPAddress GetHostIPAddress(IPAddress brainIPAddress)
        {
            IPAddress[] addresses;
            return !brainIPAddress.Equals(IPAddress.Loopback) &&
                Array.IndexOf(addresses = Dns.GetHostAddresses(Dns.GetHostName()), brainIPAddress) == -1 &&
                Array.Find(addresses, address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) is IPAddress address
                    ? address
                    : IPAddress.Loopback;
        }
    }
}
