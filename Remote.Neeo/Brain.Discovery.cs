using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo
{
    partial record Brain
    {
        /// <summary>
        /// Discovers the first <see cref="Brain"/> on the network matching the specified
        /// <paramref name="predicate"/> if provided. If no <paramref name="predicate"/> is provided, returns the first
        /// <see cref="Brain"/> discovered.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <param name="predicate">Optional predicate that must be matched by the Brain (if not <c>null</c>).</param>
        /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>.</returns>
        public static async Task<Brain?> DiscoverAsync(
            Func<Brain, bool>? predicate = default,
            CancellationToken cancellationToken = default
        )
        {
            using CancellationTokenSource tokenSource = new();
            TaskCompletionSource<Brain?> cancellationTaskSource = new();
            cancellationToken.Register(delegate
            {
                cancellationTaskSource.TrySetCanceled(cancellationToken);
                tokenSource.Cancel();
            });
            TaskCompletionSource<Brain?> brainTaskSource = new();
            return await Task.WhenAny(
                ZeroconfResolver.ResolveAsync(
                    Constants.ServiceName,
                    callback: OnHostDiscovered,
                    scanTime: TimeSpan.FromSeconds(5d),
                    cancellationToken: tokenSource.Token
                ).ContinueWith(
                    _ => /* ZeroconfResolver.ResolveAsync has completed with no matching Brain found.*/ default(Brain),
                    TaskContinuationOptions.NotOnFaulted
                ),
                brainTaskSource.Task,
                cancellationTaskSource.Task
            ).Unwrap().ConfigureAwait(false);

            void OnHostDiscovered(IZeroconfHost host)
            {
                Brain brain = Brain.Create(host);
                if (predicate != null && !predicate(brain))
                {
                    return;
                }
                tokenSource.Cancel();
                brainTaskSource.TrySetResult(brain);
            }
        }

        /// <summary>
        /// Discovers all <see cref="Brain"/>s on the network.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><see cref="Task"/> of the discovered <see cref="Brain"/>s.</returns>
        public static async Task<Brain[]> DiscoverAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(
                Constants.ServiceName,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
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
}
