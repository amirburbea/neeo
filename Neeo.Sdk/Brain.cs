﻿using System;
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
using Zeroconf;

namespace Neeo.Sdk;

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
    private static readonly TimeSpan _scanTime = TimeSpan.FromSeconds(15d);

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
    /// Discovers all <see cref="Brain"/>s on the network.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>s.</returns>
    public static async Task<Brain[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(Constants.ServiceName, Brain._scanTime, cancellationToken: cancellationToken).ConfigureAwait(false);
        return hosts.Select(Brain.TryCreateBrain).OfType<Brain>().ToArray();
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
        TaskCompletionSource<Brain?> tcs = new();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        _ = Task.Factory.StartNew(ResolveAsync, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
        return tcs.Task;

        async Task ResolveAsync()
        {
            using CancellationTokenSource raceTokenSource = new();
            try
            {
                using CancellationTokenSource junctionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    raceTokenSource.Token,
                    cancellationToken
                );
                // Bonjour sometimes fails to discover, race multiple scans at once, offset by 500ms.
                await Parallel.ForEachAsync(
                    [0, 500],
                    junctionTokenSource.Token,
                    async (delay, cancellationToken) =>
                    {
                        try
                        {
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                            await ZeroconfResolver.ResolveAsync(
                                Constants.ServiceName,
                                Brain._scanTime, callback:
                                OnHostDiscovered,
                                cancellationToken: cancellationToken
                            ).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected in race.
                        }
                    }
                ).ConfigureAwait(false);
                tcs.TrySetResult(null);
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }

            void OnHostDiscovered(IZeroconfHost host)
            {
                if (tcs.Task.IsCompleted || tcs.Task.IsCanceled || Brain.TryCreateBrain(host) is not { } brain)
                {
                    return;
                }
                if ((predicate == null || predicate(brain)) && tcs.TrySetResult(brain))
                {
                    raceTokenSource.Cancel(true);
                }
            }
        }
    }

    [GeneratedRegex(@"^(?<ip>(\d+[.]){3}\d+)[:]", RegexOptions.ExplicitCapture)]
    private static partial Regex IPAddresRegex();

    private static Brain? TryCreateBrain(IZeroconfHost host)
    {
        IService service = host.Services.Values.First();
        IReadOnlyDictionary<string, string> properties = service.Properties[0];
        IPAddress ipAddress;
        if (host.IPAddress is { } ipString)
        {
            ipAddress = IPAddress.Parse(ipString);
        }
        else if (Brain.IPAddresRegex().Match(host.Id) is { Success: true, Groups: { } groups })
        {
            ipAddress = IPAddress.Parse(groups["ip"].Value);
        }
        else
        {
            return null;
        }
        return new(ipAddress, service.Port, $"{properties["hon"]}.local", properties["rel"]);
    }

    [GeneratedRegex(@"^(?<v>\d+\.\d+)\.", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex VersionPrefixRegex();

    private static class Constants
    {
        public const string ServiceName = "_neeo._tcp.local.";
    }
}

/// <summary>
///
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
    /// <param name="name">A name for your integration server. This name should be consistent upon restarting the driver host server.</param>
    /// <param name="devices">An array of devices to register with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// </param>
    /// <param name="port">The port to listen on, if 0 the port will be assigned randomly.</param>
    /// <param name="configureLogging">By default, the integration server logs via debug in development. This allows overriding the behavior with a custom log configuration.</param>
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
    /// <param name="name">A name for your integration server. This name should be consistent upon restarting the driver host server.</param>
    /// <param name="providers">An array of device providers from which to register devices with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// </param>
    /// <param name="port">The port to listen on, if 0 the port will be assigned randomly.</param>
    /// <param name="configureLogging">By default, the integration server logs via debug in development. This allows overriding the behavior with a custom log configuration.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public static Task<ISdkEnvironment> StartServerAsync(
        this Brain brain,
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
        return brain.StartServerAsync(
            Array.ConvertAll(providers, static provider => provider.DeviceBuilder),
            name,
            hostIPAddress,
            port,
            configureLogging,
            cancellationToken
        );
    }

    internal static async ValueTask<IPAddress> GetFallbackHostIPAddressAsync(this Brain brain, CancellationToken cancellationToken)
    {
        if (IPAddress.Loopback.Equals(brain.IPAddress))
        {
            // If Brain address is loopback, use that.
            return IPAddress.Loopback;
        }
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        if (Array.IndexOf(addresses, brain.IPAddress) != -1)
        {
            // If Brain is running on this device, use loopback.
            return IPAddress.Loopback;
        }
        // Use the first IPv4 address found, or loopback.
        return Array.Find(addresses, static address => !IPAddress.IsLoopback(address)) is { } address
            ? address
            : IPAddress.Loopback;
    }
}
