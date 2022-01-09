using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo;

partial class Brain
{
    private static readonly TimeSpan _scanTime = TimeSpan.FromSeconds(5d);

    /// <summary>
    /// Discovers the first <see cref="Brain"/> on the network matching the specified
    /// <paramref name="predicate"/> if provided. If no <paramref name="predicate"/> is provided, returns the first
    /// <see cref="Brain"/> discovered.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="predicate">Optional predicate that must be matched by the Brain (if not <c>null</c>).</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>.</returns>
    public static async Task<Brain?> DiscoverAsync(Func<Brain, bool>? predicate = default, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<Brain?> brainTaskSource = new();
        IObservable<IZeroconfHost> observable = ZeroconfResolver.Resolve(Constants.ServiceName, Brain._scanTime);
        cancellationToken.Register(() => brainTaskSource.TrySetCanceled(cancellationToken));
        using (observable.Subscribe(OnHostDiscovered, () => brainTaskSource.TrySetResult(default)))
        {
            return await brainTaskSource.Task.ConfigureAwait(false);
        }

        void OnHostDiscovered(IZeroconfHost host)
        {
            Brain brain = Brain.Create(host);
            if (predicate == null || predicate(brain))
            {
                brainTaskSource.TrySetResult(brain);
            }
        }
    }

    /// <summary>
    /// Discovers all <see cref="Brain"/>s on the network.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>s.</returns>
    public static async Task<Brain[]> DiscoverAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(Constants.ServiceName, Brain._scanTime, cancellationToken: cancellationToken).ConfigureAwait(false);
        return hosts.Select(Brain.Create).ToArray();
    }

    private static Brain Create(IZeroconfHost host)
    {
        IService service = host.Services[Constants.ServiceName];
        IReadOnlyDictionary<string, string> properties = service.Properties[0];
        return new Brain(
            IPAddress.Parse(host.IPAddress),
            service.Port,
            host.DisplayName,
            properties["hon"],
            properties["rel"],
            properties["reg"]
        );
    }

    private static class Constants
    {
        public const string ServiceName = "_neeo._tcp.local.";
    }
}
