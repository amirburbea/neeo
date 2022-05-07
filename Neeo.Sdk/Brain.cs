using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Utilities;
using Zeroconf;

namespace Neeo.Sdk;

/// <summary>
/// Returns information about and contains methods for interacting with the NEEO Brain.
/// </summary>
public sealed class Brain : IAsyncDisposable, IBrain
{
    private static readonly TimeSpan _scanTime = TimeSpan.FromSeconds(15d);
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
        if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
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
    /// Discovers all <see cref="Brain"/>s on the network.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>s.</returns>
    public static async Task<Brain[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(Constants.ServiceName, Brain._scanTime, cancellationToken: cancellationToken).ConfigureAwait(false);
        Brain[] brains = new Brain[hosts.Count];
        for (int i = 0; i < brains.Length; i++)
        {
            brains[i] = Brain.CreateBrain(hosts[i]);
        }
        return brains;
    }

    /// <summary>
    /// Discovers the first <see cref="Brain"/> on the network matching the specified
    /// <paramref name="predicate"/> if provided. If no <paramref name="predicate"/> is provided, returns the first
    /// <see cref="Brain"/> discovered.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="predicate">Optional predicate that must be matched by the Brain (if not <see langword="null"/>).</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>.</returns>
    public static Task<Brain?> DiscoverOneAsync(Func<Brain, bool>? predicate = default, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<Brain?> brainTaskSource = new();
        cancellationToken.Register(() => brainTaskSource.TrySetCanceled(cancellationToken));
        ZeroconfResolver.Resolve(Constants.ServiceName, Brain._scanTime).Subscribe(
            OnHostDiscovered,
            () => brainTaskSource.TrySetResult(default),
            cancellationToken
        );
        return brainTaskSource.Task;

        void OnHostDiscovered(IZeroconfHost host)
        {
            Brain brain = Brain.CreateBrain(host);
            if (predicate == null || predicate(brain))
            {
                brainTaskSource.TrySetResult(brain);
            }
        }
    }

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
    /// </param>
    /// <param name="port">The port to listen on, if 0 the port will be assigned randomly.</param>
    /// <param name="configureLogging">By default, the integration server logs via debug in development. This allows overriding the behavior with a custom log configuration.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task<ISdkEnvironment> StartServerAsync(
        IDeviceBuilder[] devices,
        string? name = default,
        IPAddress? hostIPAddress = null,
        ushort port = 0,
        Action<HostBuilderContext, ILoggingBuilder>? configureLogging = default,
        CancellationToken cancellationToken = default
    )
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
            this,
            devices,
            $"src-{UniqueNameGenerator.Generate(name ?? Dns.GetHostName())}",
            hostIPAddress ?? await this.GetFallbackHostIPAddress(cancellationToken).ConfigureAwait(false),
            port,
            configureLogging,
            cancellationToken
        ).ConfigureAwait(false);
        return this._host.Services.GetRequiredService<ISdkEnvironment>();
    }

    /// <summary>
    /// Asynchronously starts the SDK integration server and registers it on the NEEO Brain.
    /// </summary>
    /// <param name="name">A name for your integration server. This name should be consistent upon restarting the driver host server.</param>
    /// <param name="providers">An array of device providers from which to register devices with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// </param>
    /// <param name="port">The port to listen on, if 0 the port will be assigned randomly.</param>
    /// <param name="configureLogging">By default, the integration server logs via debug in development. This allows overriding the behavior with a custom log configuration.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public Task<ISdkEnvironment> StartServerAsync(
        IDeviceProvider[] providers,
        string? name = default,
        IPAddress? hostIPAddress = null,
        ushort port = 0,
        Action<HostBuilderContext, ILoggingBuilder>? configureLogging = default,
        CancellationToken cancellationToken = default
    )
    {
        if (providers is not { Length: > 0 })
        {
            throw new ArgumentException("At least one device is required.", nameof(providers));
        }
        return this.StartServerAsync(
            Array.ConvertAll(providers, static provider => provider.DeviceBuilder),
            name,
            hostIPAddress,
            port,
            configureLogging,
            cancellationToken
        );
    }

    /// <summary>
    /// Asynchronously stops the SDK integration server and unregisters it from the NEEO Brain.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        using IHost? host = Interlocked.Exchange(ref this._host, default);
        await (host?.StopAsync(cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    private static Brain CreateBrain(IZeroconfHost host)
    {
        IService service = host.Services.Values.First();
        IReadOnlyDictionary<string, string> properties = service.Properties[0];
        return new(IPAddress.Parse(host.IPAddress), service.Port, $"{properties["hon"]}.local", properties["rel"]);
    }

    private async ValueTask<IPAddress> GetFallbackHostIPAddress(CancellationToken cancellationToken)
    {
        if (IPAddress.IsLoopback(this.IPAddress))
        {
            return this.IPAddress;
        }
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        return Array.IndexOf(addresses, this.IPAddress) != -1 || Array.Find(addresses, address => !IPAddress.IsLoopback(address)) is not { } address
            ? IPAddress.Loopback // If the Brain is running locally, we can just use localhost.
            : address;
    }

    private static class Constants
    {
        public const string ServiceName = "_neeo._tcp.local.";
    }
}

/// <summary>
/// Minimal information about a NEEO Brain.
/// </summary>
internal interface IBrain
{
    /// <summary>
    /// The host name of the NEEO Brain.
    /// </summary>
    string HostName { get; }

    /// <summary>
    /// The IP Address and port on which the NEEO Brain Service is running.
    /// </summary>
    IPEndPoint ServiceEndPoint { get; }
}