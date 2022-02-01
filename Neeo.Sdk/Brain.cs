using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk;

/// <summary>
/// Returns information about and contains methods for interacting with the NEEO Brain.
/// </summary>
public sealed class Brain : IAsyncDisposable
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

    async ValueTask IAsyncDisposable.DisposeAsync() => await this.StopServerAsync().ConfigureAwait(false);

    /// <summary>
    /// Opens the default browser to the Brain WebUI.
    /// </summary>
    public void OpenWebUI() => Process.Start(startInfo: new($"http://{this.IPAddress}:3200/eui") { UseShellExecute = true })!.Dispose();

    /// <summary>
    /// Asynchronously starts the SDK integration server and registers it on the NEEO Brain.
    /// </summary>
    /// <param name="name">A name for your integration server. This name should be consistent upon restarting the driver host server.</param>
    /// <param name="devices">An array of devices to register with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// <para />
    /// Note: If in development and the port is not 0, the server also listens on localhost at the specified <paramref name="port"/>.
    /// </param>
    /// <param name="port">The port to listen on.</param>
    /// <param name="consoleLogging">
    /// The integration server logs via debug in development. If specified as <see langword="true"/>, enables console logging (regardless of hosting environment).
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task<ISdkEnvironment> StartServerAsync(IDeviceBuilder[] devices, string? name = default, IPAddress? hostIPAddress = null, ushort port = 0, bool consoleLogging = false, CancellationToken cancellationToken = default)
    {
        if (devices is not { Length: > 0 })
        {
            throw new ArgumentException("At least one device is required.", nameof(devices));
        }
        if (this._host is not null)
        {
            throw new InvalidOperationException("Server is already running.");
        }
        this._host = await Server.StartSdkAsync(
            new(this, devices, $"src-{UniqueNameGenerator.Generate(name ?? Dns.GetHostName())}"),
            hostIPAddress ?? await Brain.GetFallbackHostIPAddress(this.IPAddress, cancellationToken).ConfigureAwait(false),
            port,
            consoleLogging,
            cancellationToken
        ).ConfigureAwait(false);
        return this._host.Services.GetRequiredService<ISdkEnvironment>();
    }

    /// <summary>
    /// Asynchronously stops the SDK integration server and unregisters it on the NEEO Brain.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        using IHost? host = Interlocked.Exchange(ref this._host, default);
        await (host?.StopAsync(cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    private static async ValueTask<IPAddress> GetFallbackHostIPAddress(IPAddress brainIPAddress, CancellationToken cancellationToken)
    {
        if (IPAddress.IsLoopback(brainIPAddress))
        {
            // If the Brain is running locally, we can just use localhost.
            return brainIPAddress;
        }
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        return Array.IndexOf(addresses, brainIPAddress) == -1 && Array.Find(addresses, address => !IPAddress.IsLoopback(address)) is { } address
            ? address
            : IPAddress.Loopback;
    }
}