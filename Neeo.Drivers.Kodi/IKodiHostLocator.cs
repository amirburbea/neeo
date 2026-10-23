using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Neeo.Drivers.Kodi;

/// <summary>
/// Finds Kodi instances on the network, by scanning for hosts that advertise their JSON-RPC HTTP service over mDNS
/// or by resolving the name of a host that is already known.
/// </summary>
public interface IKodiHostLocator
{
    /// <summary>
    /// Scans for <paramref name="scanTime"/>, calling <paramref name="onHostDiscovered"/> for each host as it is found.
    /// </summary>
    /// <param name="scanTime">How long to scan for.</param>
    /// <param name="onHostDiscovered">Called (possibly concurrently) for each host discovered.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> that completes when the scan ends.</returns>
    Task ScanAsync(TimeSpan scanTime, Action<IZeroconfHost> onHostDiscovered, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the IPv4 address of a host name.
    /// </summary>
    /// <param name="hostName">The name of the host.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The address, or <see langword="null"/> if the name could not be resolved.</returns>
    Task<IPAddress?> ResolveAsync(string hostName, CancellationToken cancellationToken);
}

internal sealed class ZeroconfKodiHostLocator : IKodiHostLocator
{
    public const string HttpServiceName = "_xbmc-jsonrpc-h._tcp.local.";

    public Task ScanAsync(TimeSpan scanTime, Action<IZeroconfHost> onHostDiscovered, CancellationToken cancellationToken) => ZeroconfResolver.ResolveAsync(
        ZeroconfKodiHostLocator.HttpServiceName,
        scanTime,
        callback: onHostDiscovered,
        cancellationToken: cancellationToken
    );

    public async Task<IPAddress?> ResolveAsync(string hostName, CancellationToken cancellationToken)
    {
        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false);
            return Array.Find(addresses, static address => address.AddressFamily is AddressFamily.InterNetwork);
        }
        catch
        {
            return null;
        }
    }
}
