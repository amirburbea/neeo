using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Remote.Neeo.Devices;
using Remote.Neeo.Web;
using Zeroconf;

namespace Remote.Neeo
{
    public static class NeeoApi
    {
        public static IDeviceBuilder CreateDevice(string name, DeviceType deviceType) => new DeviceBuilder(name) { Type = deviceType };

        public static async Task<Brain?> DiscoverBrainAsync(Func<Brain, bool>? predicate = default)
        {
            TaskCompletionSource<Brain?> taskCompletionSource = new();
            using CancellationTokenSource cancellationTokenSource = new();
            return await Task.WhenAny(
                ZeroconfResolver.ResolveAsync(
                    Constants.ServiceName,
                    callback: OnHostDiscovered,
                    cancellationToken: cancellationTokenSource.Token
                ).ContinueWith(
                    _ => default(Brain), // ZeroconfResolver.ResolveAsync has completed with no matching Brain found.
                    TaskContinuationOptions.NotOnFaulted
                ),
                taskCompletionSource.Task
            ).Unwrap().ConfigureAwait(false);

            void OnHostDiscovered(IZeroconfHost host)
            {
                Brain brain = NeeoApi.CreateBrain(host);
                if (predicate != null && !predicate(brain))
                {
                    return;
                }
                cancellationTokenSource.Cancel();
                taskCompletionSource.TrySetResult(brain);
            }
        }

        public static async Task<Brain[]> DiscoverBrainsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(
                Constants.ServiceName, 
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            Brain[] array = new Brain[hosts.Count];
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = NeeoApi.CreateBrain(hosts[index]);
            }
            return array;
        }

        public static Task StartServerAsync(Brain brain, string name, params IDeviceBuilder[] devices) => NeeoApi.StartServerAsync(brain, name, devices, cancellationToken: default);

        public static Task StartServerAsync(Brain brain, string name, IDeviceBuilder[] devices, string? ipAddress = default, int port = 8080, CancellationToken cancellationToken = default)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Non-blank name is required.", nameof(name));
            }
            if (devices == null || devices.Length == 0 || Array.IndexOf(devices, default) != -1)
            {
                throw new ArgumentException("Devices collection can not be null/empty or contain null.", nameof(devices));
            }
            if (port < 0 || port > ushort.MaxValue)
            {
                throw new ArgumentException("Invalid port.", nameof(port));
            }
            return Server.StartAsync(
                brain,
                name,
                devices,
                ipAddress is null ? GetIPAddress(brain.IPAddress) : IPAddress.Parse(ipAddress),
                port,
                cancellationToken
            );

            static IPAddress GetIPAddress(IPAddress brainIPAddress)
            {
                IPAddress[] addresses;
                return !brainIPAddress.Equals(IPAddress.Loopback) && 
                    Array.IndexOf(addresses = Dns.GetHostAddresses(Dns.GetHostName()), brainIPAddress) == -1 && 
                    Array.Find(addresses, address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) is IPAddress address 
                        ? address 
                        : IPAddress.Loopback;
            }
        }

        public static Task StopServerAsync(CancellationToken cancellationToken = default) => Server.StopAsync(cancellationToken);

        private static Brain CreateBrain(IZeroconfHost host)
        {
            IService service = host.Services[Constants.ServiceName];
            IReadOnlyDictionary<string, string> properties = service.Properties[0];
            return new Brain(
                IPAddress.Parse(host.IPAddress),
                service.Port,
                host.DisplayName,
                $"{properties["hon"]}.local",
                properties["rel"],
                properties["reg"],
                DateTime.ParseExact(
                    properties["upd"],
                    "yyyy-M-d",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                )
            );
        }

        internal static class Constants
        {
            public const string ServiceName = "_neeo._tcp.local.";
        }
    }
}
