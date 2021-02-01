using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo
{
    public static class BrainDiscovery
    {
        public static async Task<BrainDescriptor[]> DiscoverBrainsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(Constants.ServiceName, cancellationToken: cancellationToken).ConfigureAwait(false);
            BrainDescriptor[] array = new BrainDescriptor[hosts.Count];
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = BrainDiscovery.CreateDescriptor(hosts[index]);
            }
            return array;
        }

        public static async Task<BrainDescriptor?> GetFirstBrainAsync(Func<BrainDescriptor, bool>? predicate = default)
        {
            using CancellationTokenSource tokenSource = new();
            TaskCompletionSource<BrainDescriptor?> taskSource = new();

            void OnHostDiscovered(IZeroconfHost host)
            {
                BrainDescriptor descriptor = BrainDiscovery.CreateDescriptor(host);
                if (predicate != null && !predicate(descriptor))
                {
                    return;
                }
                tokenSource.Cancel();
                taskSource.TrySetResult(descriptor);
            }

            return await Task.WhenAny(
                ZeroconfResolver.ResolveAsync(
                    Constants.ServiceName,
                    callback: OnHostDiscovered,
                    cancellationToken: tokenSource.Token
                ).ContinueWith(
                    _ => default(BrainDescriptor), // If ResolveAsync has completed, no matching Brain was found.
                    TaskContinuationOptions.NotOnFaulted
                ),
                taskSource.Task
            ).Unwrap().ConfigureAwait(false);
        }

        private static BrainDescriptor CreateDescriptor(IZeroconfHost host)
        {
            IService service = host.Services[Constants.ServiceName];
            IReadOnlyDictionary<string, string> properties = service.Properties[0];
            return new BrainDescriptor(
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
