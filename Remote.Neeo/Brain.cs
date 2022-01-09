using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Remote.Neeo.Devices;
using Remote.Neeo.Rest;

namespace Remote.Neeo;

/// <summary>
/// Returns information about and contains methods for interacting with the NEEO Brain.
/// </summary>
public sealed partial class Brain
{
    private static readonly Regex _versionPrefixRegex = new(@"^(?<v>0.\d+)\.", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private IHost? _host;

    /// <summary>
    /// Initializes an instance of the <see cref="Brain"/> class with details about the NEEO Brain.
    /// </summary>
    /// <param name="ipAddress">The IP Address of the NEEO Brain on the network.</param>
    /// <param name="servicePort">The port on which the NEEO Brain service is running.</param>
    /// <param name="name">The name assigned to the NEEO Brain by the end user.</param>
    /// <param name="hostName">The host name of the NEEO Brain.</param>
    /// <param name="version">The firmware version of the NEEO Brain.</param>
    /// <param name="region">The region set in the NEEO Brain firmware.<para/>Example: &quot;US&quot;.</param>
    private Brain(IPAddress ipAddress, int servicePort, string name, string hostName, string version, string region)
    {
        (this.IPAddress, this.ServicePort, this.Name, this.HostName, this.Version, this.Region) = (
            ipAddress ?? throw new ArgumentNullException(nameof(ipAddress)),
            servicePort,
            name ?? throw new ArgumentNullException(nameof(name)),
            hostName ?? throw new ArgumentNullException(nameof(hostName)),
            version ?? throw new ArgumentNullException(nameof(version)),
            region ?? throw new ArgumentNullException(nameof(region))
        );
    }

    /// <summary>
    /// Gets a value indicating if the Brain firmware version is sufficient for running the SDK.
    /// The Brain must be running firmware <c>v0.50</c> or above.
    /// </summary>
    public bool HasCompatibleFirmware => double.Parse(Brain._versionPrefixRegex.Match(this.Version).Groups["v"].Value, CultureInfo.InvariantCulture) > 0.5;

    /// <summary>
    /// The host name of the NEEO Brain.
    /// </summary>
    public string HostName { get; }

    /// <summary>
    /// The IP Address of the NEEO Brain on the network.
    /// </summary>
    public IPAddress IPAddress { get; }

    /// <summary>
    /// The name assigned to the NEEO Brain by the end user.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The region set in the NEEO Brain firmware.<para/>Example: &quot;US&quot;.
    /// </summary>
    public string Region { get; }

    /// <summary>
    /// The endpoint on which the NEEO Brain API is running.
    /// </summary>
    public IPEndPoint ServiceEndPoint => new(this.IPAddress, this.ServicePort);

    /// <summary>
    /// The port on which the NEEO Brain API is running.
    /// </summary>
    public int ServicePort { get; }

    /// <summary>
    /// The firmware version of the NEEO Brain.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Opens the default browser to the Brain WebUI.
    /// </summary>
    public void OpenWebUI() => Process.Start(startInfo: new($"http://{this.IPAddress}:3200/eui") { UseShellExecute = true })?.Dispose();

    /// <summary>
    /// Asynchronously starts the SDK integration server and registers it on the NEEO Brain.
    /// </summary>
    /// <param name="name">A name for your integration server. This name should be consistent upon restarting the driver host server.</param>
    /// <param name="devices">An array of devices to register with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// <para />
    /// Note: If in development, the server also listens on localhost at the specified <paramref name="port"/>.
    /// </param>
    /// <param name="port">The port to listen on.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task StartServerAsync(string name, IDeviceBuilder[] devices, IPAddress? hostIPAddress = null, int port = 9000, CancellationToken cancellationToken = default)
    {
        if (this._host != null)
        {
            throw new InvalidOperationException("Server is already running.");
        }
        if (!this.HasCompatibleFirmware)
        {
            throw new InvalidOperationException("The NEEO Brain is not running a compatible firmware version.  It must be upgraded first.");
        }

        IPAddress GetFallbackHostIPAddress()
        {
            if (this.IPAddress != IPAddress.Loopback)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                if (Array.IndexOf(addresses, this.IPAddress) == -1 && Array.Find(addresses, address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) is { } address)
                {
                    return address;
                }
            }
            return IPAddress.Loopback;
        }

        this._host = await Server.StartAsync(this, name, devices, hostIPAddress ?? GetFallbackHostIPAddress(), port, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously stops the SDK integration server and unregisters it from the Brain.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public Task StopServerAsync(CancellationToken cancellationToken = default) => Interlocked.Exchange(ref this._host, null) is { } host
        ? Server.StopAsync(host, cancellationToken)
        : Task.CompletedTask;
}