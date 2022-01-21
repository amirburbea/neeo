using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neeo.Api.Devices;
using Neeo.Api.Rest;

namespace Neeo.Api;

/// <summary>
/// Returns information about and contains methods for interacting with the NEEO Brain.
/// </summary>
public sealed class Brain
{
    private static readonly Regex _versionPrefixRegex = new(@"^0\.(?<v>\d+)\.", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private IHost? _host;

    /// <summary>
    /// Initializes an instance of the <see cref="Brain"/> class with details about the NEEO Brain.
    /// </summary>
    /// <param name="ipAddress">The IP Address of the NEEO Brain on the network.</param>
    /// <param name="servicePort">The port on which the NEEO Brain service is running.</param>
    /// <param name="hostName">The host name of the NEEO Brain.</param>
    /// <param name="version">The firmware version of the NEEO Brain.</param>
    public Brain(IPAddress ipAddress, int servicePort = 3000, string? hostName = default, string version = "0.50.0")
    {
        if (ipAddress is not { AddressFamily: AddressFamily.InterNetwork })
        {
            throw new ArgumentException("The supplied IP address must be an IPv4 address.", nameof(ipAddress));
        }
        if (Brain._versionPrefixRegex.Match(version) is not { Success: true } match || int.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture) < 50)
        {
            throw new InvalidOperationException("The NEEO Brain is not running a compatible firmware version (>= 0.50). It must be upgraded first.");
        }
        (this.IPAddress, this.ServicePort, this.HostName) = (ipAddress, servicePort, hostName ?? ipAddress.ToString());
    }

    /// <summary>
    /// The host name of the NEEO Brain.
    /// </summary>
    public string HostName { get; }

    /// <summary>
    /// The IP Address of the NEEO Brain on the network.
    /// </summary>
    public IPAddress IPAddress { get; }

    /// <summary>
    /// The port on which the NEEO Brain API is running.
    /// </summary>
    public int ServicePort { get; }

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
    public async Task StartServerAsync(IReadOnlyCollection<IDeviceBuilder> devices, string? name = default, IPAddress? hostIPAddress = null, int port = 9000, CancellationToken cancellationToken = default)
    {
        if (this._host is not null)
        {
            throw new InvalidOperationException("Server is already running.");
        }
        this._host = await Server.StartAsync(this, name ?? Dns.GetHostName(), devices, hostIPAddress, port, cancellationToken).ConfigureAwait(false);
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