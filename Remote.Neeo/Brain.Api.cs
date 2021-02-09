using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Remote.Neeo
{
    partial record Brain
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        internal Task RegisterServerAsync(string name, string baseUrl, CancellationToken cancellationToken = default) => this.PostAsync(
            "api/registerSdkDeviceAdapter",
            new { Name = name, BaseUrl = baseUrl },
            cancellationToken
        );

        internal Task UnregisterServerAsync(string name, CancellationToken cancellationToken = default) => this.PostAsync(
            "api/unregisterSdkDeviceAdapter",
            new { Name = name },
            cancellationToken
        );

        private async Task<string> GetAsync(string apiPath, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.GetAsync(this.GetUri(apiPath), cancellationToken).ConfigureAwait(false);
            return await Brain.GetContentAsync(message, cancellationToken).ConfigureAwait(false);
        }

        private static Task<string> GetContentAsync(HttpResponseMessage message, CancellationToken cancellationToken)
        {
            return message.StatusCode != HttpStatusCode.OK && message.StatusCode != HttpStatusCode.NoContent
                ? throw new WebException($"Server returned {message.StatusCode}.")
                : message.Content.ReadAsStringAsync(cancellationToken);
        }

        private string GetUri(string apiPath) => $"http://{this.HostName}:{this.Port}/v1/{apiPath}";

        private async Task<string> PostAsync<TBody>(string apiPath, TBody body, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.PostAsync(
                this.GetUri(apiPath),
                new StringContent(JsonSerializer.Serialize(body, Brain._jsonOptions), Encoding.UTF8, "application/json"),
                cancellationToken
            ).ConfigureAwait(false);
            return await Brain.GetContentAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
