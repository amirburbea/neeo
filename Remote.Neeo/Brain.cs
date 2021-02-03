using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo
{
    public record Brain
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        internal Brain(IPAddress ipAddress, int port, string name, string hostName, string version, string region, DateTime updated)
        {
            (this.IPAddress, this.Port, this.Name, this.HostName, this.Version, this.Region, this.Updated) = (ipAddress, port, name, hostName, version, region, updated);
        }

        public string HostName { get; }

        public IPAddress IPAddress { get; }

        public string Name { get; }

        public int Port { get; }

        public string Region { get; }

        public DateTime Updated { get; }

        public string Version { get; }

        public static async Task<Brain?> DiscoverAsync(Func<Brain, bool>? predicate = default)
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
                Brain brain = Brain.Create(host);
                if (predicate != null && !predicate(brain))
                {
                    return;
                }
                cancellationTokenSource.Cancel();
                taskCompletionSource.TrySetResult(brain);
            }
        }

        public static async Task<Brain[]> DiscoverAllAsync()
        {
            IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(Constants.ServiceName).ConfigureAwait(false);
            Brain[] array = new Brain[hosts.Count];
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = Brain.Create(hosts[index]);
            }
            return array;
        }

        internal async Task<string> GetAsync(string apiPath, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.GetAsync(this.GetApiUri(apiPath), cancellationToken).ConfigureAwait(false);
            return await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        internal async Task<string> PostAsync<T>(string apiPath, T body, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.PostAsync(
                this.GetApiUri(apiPath), 
                new StringContent(JsonSerializer.Serialize(body, Brain._jsonOptions), Encoding.UTF8, "application/json"),
                cancellationToken
            );
            return await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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

        private string GetApiUri(string path) => $"http://{this.HostName}:{this.Port}/v1/api/{path}";

        private static class Constants
        {
            public const string ServiceName = "_neeo._tcp.local.";
        }
    }
}
