using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Rest;
using Zeroconf;

namespace Neeo.Sdk;

/// <summary>
/// Minimal information about a NEEO Brain.
/// </summary>
public interface IBrain
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

/// <summary>
/// Returns information about and contains methods for interacting with the NEEO Brain.
/// </summary>
/// <remarks>
/// Initializes an instance of the <see cref="Brain"/> class with details about the NEEO Brain.
/// </remarks>
/// <param name="ipAddress">The IP Address of the NEEO Brain on the network.</param>
/// <param name="servicePort">The port on which the NEEO Brain service is running.</param>
/// <param name="hostName">The host name of the NEEO Brain.</param>
/// <param name="version">The firmware version of the NEEO Brain.</param>
public sealed partial class Brain(
    IPAddress ipAddress,
    int servicePort = 3000,
    string? hostName = default,
    string version = "0.50.0"
) : IBrain
{
    /// <summary>
    /// The host name of the NEEO Brain.
    /// </summary>
    public string HostName { get; } = hostName ?? ipAddress.ToString();

    /// <summary>
    /// The IP Address on which the NEEO Brain Service is running.
    /// </summary>
    public IPAddress IPAddress => this.ServiceEndPoint.Address;

    /// <summary>
    /// The IP Address and port on which the NEEO Brain Service is running.
    /// </summary>
    public IPEndPoint ServiceEndPoint { get; } = new(
        ipAddress.AddressFamily == AddressFamily.InterNetwork
            ? ipAddress
            : throw new ArgumentException("The supplied IP address must be an IPv4 address.", nameof(ipAddress)),
        servicePort
    );

    /// <summary>
    /// The firmware version of the NEEO Brain.
    /// </summary>
    public string Version { get; } = Brain.VersionPrefixRegex().Match(version) is { Success: true, Groups: { } groups } && double.Parse(groups["v"].Value, CultureInfo.InvariantCulture) >= 0.5d
        ? version
        : throw new InvalidOperationException("The NEEO Brain is not running a compatible firmware version (>= 0.50). It must be upgraded first.");

    /// <summary>
    /// Discovers all <see cref="Brain"/> s on the network.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/> s.</returns>
    public static async Task<Brain[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        if (!Brain.IsNetworkConnected())
        {
            throw new ApplicationException("Brain discovery requires network connectivity");
        }
        IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(
            Constants.ServiceName,
            scanTime: TimeSpan.FromSeconds(15),
            retries: 1,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        return [.. hosts.Select(Brain.TryCreateBrain).OfType<Brain>()];
    }

    /// <summary>
    /// Discovers the first <see cref="Brain"/> on the network matching the specified <paramref
    /// name="predicate"/> if provided. If no <paramref name="predicate"/> is provided, returns the
    /// first <see cref="Brain"/> discovered.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="predicate">
    /// Optional predicate that must be matched by the Brain (if not <see langword="null"/>).
    /// </param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>.</returns>
    public static Task<Brain?> DiscoverOneAsync(Func<Brain, bool>? predicate = default, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<Brain?> tcs = new();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        if (!Brain.IsNetworkConnected())
        {
            tcs.TrySetException(new ApplicationException("Brain discovery requires network connectivity"));
        }
        else
        {
            _ = Task.Run(ResolveAsync, cancellationToken);
        }
        return tcs.Task;

        async Task ResolveAsync()
        {
            try
            {
                // Running for up to 1 + 2 + 4 + 8 = 15 seconds.
                for (int seconds = 1; !tcs.Task.IsCompleted && seconds <= 8; seconds *= 2)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(timeSpan);
                    try
                    {
                        await ZeroconfResolver.ResolveAsync(
                            Constants.ServiceName,
                            scanTime: timeSpan,
                            retries: 1,
                            callback: host => OnHostDiscovered(host, cts),
                            cancellationToken: cts.Token
                        ).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        break;
                    }
                    catch (Exception)
                    {
                        // Retry on other errors.
                    }
                }
            }
            finally
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetResult(null);
                }
            }

            void OnHostDiscovered(IZeroconfHost host, CancellationTokenSource cancellationTokenSource)
            {
                if (Brain.TryCreateBrain(host) is { } brain && (predicate == null || predicate(brain)) && tcs.TrySetResult(brain))
                {
                    // Cancel to break out of the discovery process.
                    cancellationTokenSource.Cancel(true);
                }
            }
        }
    }

    [GeneratedRegex(@"^(?<ip>(\d+[.]){3}\d+)[:]", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex IPAddresRegex();

    private static bool IsNetworkConnected() => Enumerable.Any(
        from netInterface in NetworkInterface.GetAllNetworkInterfaces()
        where (netInterface.OperationalStatus, netInterface.NetworkInterfaceType) is (OperationalStatus.Up, NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
        where netInterface.GetIPProperties().GatewayAddresses.Count != 0
        select netInterface
    );

    private static Brain? TryCreateBrain(IZeroconfHost host)
    {
        if (host.Services.Values.FirstOrDefault() is not { Port: int port, Properties: [{ } properties, ..] })
        {
            return null;
        }
        string hostName = $"{properties["hon"]}.local";
        string version = properties["rel"];
        return (host.IPAddress, host.Id) switch
        {
            ({ Length: > 0 } ip, _) => CreateBrain(ip),
            (_, { } id) when Brain.IPAddresRegex().Match(id) is { Success: true, Groups: { } groups } => CreateBrain(groups["ip"].Value),
            _ => null
        };

        Brain CreateBrain(string ip) => new(IPAddress.Parse(ip), port, hostName, version);
    }

    [GeneratedRegex(@"^(?<v>\d+\.\d+)\.", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex VersionPrefixRegex();

    private static class Constants
    {
        public const string ServiceName = "_neeo._tcp.local.";
    }
}

/// <summary>
/// </summary>
public static class BrainMethods
{
    /// <summary>
    /// Opens the default browser to the Brain WebUI.
    /// </summary>
    /// <param name="brain">The NEEO Brain.</param>
    public static void OpenWebUI(this Brain brain) => Process.Start(
        startInfo: new($"http://{(brain ?? throw new ArgumentNullException(nameof(brain))).IPAddress}:3200/eui") { UseShellExecute = true }
    )?.Dispose();

    /// <summary>
    /// Asynchronously starts the SDK integration server and registers it on the NEEO Brain.
    /// </summary>
    /// <param name="brain">The NEEO Brain.</param>
    /// <param name="name">
    /// A name for your integration server. This name should be consistent upon restarting the
    /// driver host server.
    /// </param>
    /// <param name="devices">An array of devices to register with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the
    /// first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// </param>
    /// <param name="port">The port to listen on, if 0 the port will be assigned randomly.</param>
    /// <param name="configureLogging">
    /// By default, the integration server logs via debug in development. This allows overriding the
    /// behavior with a custom log configuration.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public static async Task<ISdkEnvironment> StartServerAsync(
        this Brain brain,
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
        IHost host = await Server.StartSdkAsync(
            brain ?? throw new ArgumentNullException(nameof(brain)),
            devices,
            name ?? brain.HostName,
            hostIPAddress ?? await brain.GetFallbackHostIPAddressAsync(cancellationToken).ConfigureAwait(false),
            port,
            configureLogging,
            cancellationToken
        ).ConfigureAwait(false);
        return host.Services.GetRequiredService<ISdkEnvironment>();
    }

    /// <summary>
    /// Asynchronously starts the SDK integration server and registers it on the NEEO Brain.
    /// </summary>
    /// <param name="brain">The NEEO Brain.</param>
    /// <param name="name">
    /// A name for your integration server. This name should be consistent upon restarting the
    /// driver host server.
    /// </param>
    /// <param name="devices">An array of devices to register with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the
    /// first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// </param>
    /// <param name="port">The port to listen on, if 0 the port will be assigned randomly.</param>
    /// <param name="configureLogging">
    /// By default, the integration server logs via debug in development. This allows overriding the
    /// behavior with a custom log configuration.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public static Task<ISdkEnvironment> StartServerAsync(
        this Brain brain,
        IDeviceProvider[] devices,
        string? name = default,
        IPAddress? hostIPAddress = null,
        ushort port = 0,
        Action<HostBuilderContext, ILoggingBuilder>? configureLogging = default,
        CancellationToken cancellationToken = default
    )
    {
        return brain.StartServerAsync(
            devices is not { Length: > 0 } providers
                ? throw new ArgumentException("At least one device is required.", nameof(devices))
                : [.. providers.Select(provider => provider.DeviceBuilder)],
            name,
            hostIPAddress,
            port,
            configureLogging,
            cancellationToken
        );
    }

    internal static async ValueTask<IPAddress> GetFallbackHostIPAddressAsync(this Brain brain, CancellationToken cancellationToken)
    {
        if (!brain.IPAddress.Equals(IPAddress.Loopback))
        {
            // Get IPv4 addresses for the current device.
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
            // If the Brain IP is not contained, the Brain is a separate device. Return the first
            // non-loopback IP address on this host.
            if (Array.IndexOf(addresses, brain.IPAddress) == -1 && Array.Find(addresses, static address => !IPAddress.IsLoopback(address)) is { } ipAddress)
            {
                return ipAddress;
            }
        }
        return IPAddress.Loopback;
    }
}
