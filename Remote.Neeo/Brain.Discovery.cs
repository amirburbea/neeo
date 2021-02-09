using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo
{
    partial record Brain
    {
        public static async Task<Brain?> DiscoverAsync(Func<Brain, bool>? predicate = default, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource cts = new();
            TaskCompletionSource<Brain?> cancellationTask = new();
            cancellationToken.Register(delegate
            {
                cancellationTask.TrySetCanceled(cancellationToken);
                cts.Cancel();
            });
            TaskCompletionSource<Brain?> brainTask = new();
            return await Task.WhenAny(
                ZeroconfResolver.ResolveAsync(Constants.ServiceName, callback: OnHostDiscovered, cancellationToken: cts.Token).ContinueWith(
                    _ => /* ZeroconfResolver.ResolveAsync has completed with no matching Brain found.*/ default(Brain),
                    TaskContinuationOptions.NotOnFaulted
                ),
                brainTask.Task,
                cancellationTask.Task
            ).Unwrap().ConfigureAwait(false);

            void OnHostDiscovered(IZeroconfHost host)
            {
                Brain brain = Brain.Create(host);
                if (predicate != null && !predicate(brain))
                {
                    return;
                }
                cts.Cancel();
                brainTask.TrySetResult(brain);
            }
        }

        public static async Task<Brain[]> DiscoverAllAsync(CancellationToken cancellationToken = default)
        {
            return (await ZeroconfResolver.ResolveAsync(Constants.ServiceName, cancellationToken: cancellationToken).ConfigureAwait(false))
                .Select(Brain.Create)
                .ToArray();
        }

        private static Brain Create(IZeroconfHost host)
        {
            IService service = host.Services[Constants.ServiceName];
            IReadOnlyDictionary<string, string> properties = service.Properties[0];
            return new Brain(
                IPAddress.Parse(host.IPAddress),
                service.Port,
                host.DisplayName,
                $"{properties["hon"]}.local",
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

        private static class Constants
        {
            public const string ServiceName = "_neeo._tcp.local.";
        }
    }
}
