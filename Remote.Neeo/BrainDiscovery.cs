using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo
{
    public static class BrainDiscovery
    {
        public static async Task<Brain?> DiscoverBrainAsync(Func<Brain, bool>? predicate = default)
        {
            TaskCompletionSource<Brain?> taskCompletionSource = new();
            using CancellationTokenSource cancellationTokenSource = new();
            return await Task.WhenAny(
                ZeroconfResolver.ResolveAsync(
                    Constants.ServiceName,
                    callback: OnHostDiscovered,
                    cancellationToken: cancellationTokenSource.Token
                ).ContinueWith(
                    _ => default(Brain), // ZeroconfResolver.ResolveAsync has completed with no matching Brain found.
                    TaskContinuationOptions.NotOnFaulted
                ),
                taskCompletionSource.Task
            ).Unwrap().ConfigureAwait(false);

            void OnHostDiscovered(IZeroconfHost host)
            {
                Brain brain = BrainDiscovery.CreateBrain(host);
                if (predicate != null && !predicate(brain))
                {
                    return;
                }
                cancellationTokenSource.Cancel();
                taskCompletionSource.TrySetResult(brain);
            }
        }

        public static async Task<Brain[]> DiscoverBrainsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(
                Constants.ServiceName,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            Brain[] array = new Brain[hosts.Count];
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = BrainDiscovery.CreateBrain(hosts[index]);
            }
            return array;
        }

        private static Brain CreateBrain(IZeroconfHost host)
        {
            IService service = host.Services[Constants.ServiceName];
            IReadOnlyDictionary<string, string> properties = service.Properties[0];
            return new Brain(
                host.IPAddress,
                service.Port,
                host.DisplayName,
                properties["hon"],
                properties["rel"],
                properties["reg"],
                DateTime.ParseExact(
                    properties["upd"],
                    "yyyy-M-d",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                )
            );
        }

        internal static class Constants
        {
            public const string ServiceName = "_neeo._tcp.local.";
        }
    }
}
