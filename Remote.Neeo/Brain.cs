using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Remote.Neeo.Devices;
using Remote.Neeo.Web;

namespace Remote.Neeo
{
    public partial record Brain
    {
        private IHost? _host;

        public Brain(IPAddress ipAddress, int port, string name, string hostName, string version, string region, DateTime updated)
        {
            (this.IPAddress, this.Port, this.Name, this.HostName, this.Version, this.Region, this.Updated) = (
                ipAddress ?? throw new ArgumentNullException(nameof(ipAddress)),
                port,
                name ?? throw new ArgumentNullException(nameof(name)),
                hostName ?? throw new ArgumentNullException(nameof(hostName)),
                version ?? throw new ArgumentNullException(nameof(version)),
                region ?? throw new ArgumentNullException(nameof(region)),
                updated
            );
        }

        public string HostName { get; }

        public IPAddress IPAddress { get; }

        public string Name { get; }

        public int Port { get; }

        public string Region { get; }

        public DateTime Updated { get; }

        public string Version { get; }

        /// <summary>
        /// Opens the default browser to the <see cref="Brain"/> WebUI.
        /// </summary>
        public void OpenWebUI() => Process.Start(new ProcessStartInfo($"http://{this.HostName}:3200/eui") { UseShellExecute = true })?.Dispose();

        public async Task StartServerAsync(string name, IDeviceBuilder[] devices, IPAddress hostIPAddress, int port = 9000, CancellationToken cancellationToken = default)
        {
            this._host = await Server.StartAsync(this, name, devices, hostIPAddress, port, cancellationToken).ConfigureAwait(false);
        }

        public Task StartServerAsync(string name, IDeviceBuilder[] devices, int port = 9000, CancellationToken cancellationToken = default)
        {
            IPAddress GetHostIPAddress()
            {
                IPAddress[] addresses;
                return !this.IPAddress.Equals(IPAddress.Loopback) &&
                    Array.IndexOf(addresses = Dns.GetHostAddresses(Dns.GetHostName()), this.IPAddress) == -1 &&
                    Array.Find(addresses, address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) is IPAddress address
                        ? address
                        : IPAddress.Loopback;
            }

            return this.StartServerAsync(name, devices, GetHostIPAddress(), port, cancellationToken);
        }

        public Task StopServerAsync(CancellationToken cancellationToken = default) => Server.StopAsync(Interlocked.Exchange(ref this._host, null), cancellationToken);
    }
}
