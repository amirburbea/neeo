using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Neeo.Sdk;

/// <summary>
/// Returns information about and contains methods for interacting with the NEEO Brain.
/// </summary>
public sealed partial class Brain
{
    private static readonly Regex _versionPrefixRegex = new(@"^0\.(?<v>\d+)\.", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

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
        (this.ServiceEndPoint, this.HostName) = (new(ipAddress, servicePort), hostName ?? ipAddress.ToString());
    }

    /// <summary>
    /// The host name of the NEEO Brain.
    /// </summary>
    public string HostName { get; }

    /// <summary>
    /// The IP Address on which the NEEO Brain Service is running.
    /// </summary>
    public IPAddress IPAddress => this.ServiceEndPoint.Address;

    /// <summary>
    /// The IP Address and port on which the NEEO Brain Service is running.
    /// </summary>
    public IPEndPoint ServiceEndPoint { get; }

    /// <summary>
    /// Opens the default browser to the Brain WebUI.
    /// </summary>
    public void OpenWebUI() => Process.Start(startInfo: new($"http://{this.IPAddress}:3200/eui") { UseShellExecute = true })!.Dispose();
}