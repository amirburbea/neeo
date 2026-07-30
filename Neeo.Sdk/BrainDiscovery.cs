using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Neeo.Sdk;

/// <summary>
/// Discovers NEEO Brains on the network.
/// </summary>
public interface IBrainDiscovery
{
    /// <summary>
    /// Discovers all <see cref="Brain"/> s on the network.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/> s.</returns>
    Task<Brain[]> DiscoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers the first <see cref="Brain"/> on the network matching the specified <paramref
    /// name="predicate"/> if provided. If no <paramref name="predicate"/> is provided, returns the
    /// first <see cref="Brain"/> discovered.
    /// </summary>
    /// <param name="predicate">
    /// Optional predicate that must be matched by the Brain (if not <see langword="null"/>).
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>.</returns>
    Task<Brain?> DiscoverOneAsync(Func<Brain, bool>? predicate = default, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IBrainDiscovery"/>
public sealed partial class BrainDiscovery : IBrainDiscovery
{
    /// <inheritdoc/>
    public async Task<Brain[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        if (!BrainDiscovery.IsNetworkConnected())
        {
            throw new ApplicationException("Brain discovery requires network connectivity");
        }
        IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(
            Constants.ServiceName,
            scanTime: TimeSpan.FromSeconds(15),
            retries: 1,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        return [.. hosts.Select(BrainDiscovery.TryCreateBrain).OfType<Brain>()];
    }

    /// <inheritdoc/>
    public Task<Brain?> DiscoverOneAsync(Func<Brain, bool>? predicate = default, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<Brain?> taskSource = new();
        cancellationToken.Register(() => taskSource.TrySetCanceled(cancellationToken));
        if (!BrainDiscovery.IsNetworkConnected())
        {
            taskSource.TrySetException(new ApplicationException("Brain discovery requires network connectivity"));
        }
        else
        {
            _ = Task.Run(ResolveAsync, cancellationToken).ContinueWith(
                _ => taskSource.TrySetResult(null),
                CancellationToken.None, // Ensure continuation runs.
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }
        return taskSource.Task;

        async Task ResolveAsync()
        {
            // Running for up to 1 + 2 + 4 + 8 = 15 seconds.
            for (int seconds = 1; !taskSource.Task.IsCompleted && seconds <= 8; seconds *= 2)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                using CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationSource.CancelAfter(timeSpan);
                try
                {
                    await ZeroconfResolver.ResolveAsync(
                        Constants.ServiceName,
                        scanTime: timeSpan,
                        retries: 1,
                        callback: host =>
                        {
                            if (BrainDiscovery.TryCreateBrain(host) is not { } brain)
                            {
                                return;
                            }
                            if ((predicate == null || predicate(brain)) && taskSource.TrySetResult(brain))
                            {
                                // Cancel to break out of the discovery process.
                                cancellationSource.Cancel(true);
                            }
                        },
                        cancellationToken: cancellationSource.Token
                    ).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    taskSource.TrySetCanceled(cancellationToken);
                    break;
                }
                catch (Exception)
                {
                    // Retry on other errors.
                }
            }
        }
    }

    [GeneratedRegex(@"^(?<ip>(\d+[.]){3}\d+)[:]", RegexOptions.ExplicitCapture)]
    private static partial Regex IPAddressRegex();

    private static bool IsNetworkConnected() => Enumerable.Any(
        from netInterface in NetworkInterface.GetAllNetworkInterfaces()
        where (netInterface.OperationalStatus, netInterface.NetworkInterfaceType) is (OperationalStatus.Up, NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
        where netInterface.GetIPProperties().GatewayAddresses.Count != 0
        select netInterface
    );

    private static Brain? TryCreateBrain(IZeroconfHost host)
    {
        if (host.Services.Values.FirstOrDefault() is not { Properties: [{ } properties, ..] })
        {
            return null;
        }
        return (host.IPAddress, host.Id) switch
        {
            ({ Length: > 0 } ip, _) => CreateBrain(ip),
            (_, { } id) when BrainDiscovery.IPAddressRegex().Match(id) is { Success: true, Groups: { } groups } => CreateBrain(groups["ip"].Value),
            _ => null
        };

        Brain CreateBrain(string ip)
        {
            string hostName = $"{properties["hon"]}.local";
            string version = properties["rel"];
            return new(IPAddress.Parse(ip), hostName, version);
        }
    }

    private static class Constants
    {
        public const string ServiceName = "_neeo._tcp.local.";
    }
}
