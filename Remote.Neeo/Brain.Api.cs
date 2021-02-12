using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Remote.Neeo
{
    partial record Brain
    {
        /// <summary>
        /// Asynchronously fetch data via a GET request to a REST endpoint on the Brain at the specified API <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the aschronous operation.</returns>
        public async Task<string> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.GetAsync(this.GetUri(path), cancellationToken).ConfigureAwait(false);
            return await Brain.GetContentAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Dictionary<string, JsonElement>> GetSystemInfoAsync() => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            await this.GetAsync("systeminfo"),
            JsonSerialization.Options
        )!;

        /// <summary>
        /// Asynchronously fetch data via a POST request to a REST endpoint on the Brain at the specified API <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the aschronous operation.</returns>
        public async Task<string> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.PostAsync(
                this.GetUri(path),
                new StringContent(JsonSerializer.Serialize(body, JsonSerialization.Options), Encoding.UTF8, "application/json"),
                cancellationToken
            ).ConfigureAwait(false);
            return await Brain.GetContentAsync(message, cancellationToken).ConfigureAwait(false);
        }

        internal Task RegisterServerAsync(string name, string baseUrl, CancellationToken cancellationToken = default) => this.PostAsync(
            "v1/api/registerSdkDeviceAdapter",
            new { Name = name, BaseUrl = baseUrl },
            cancellationToken
        );

        internal Task UnregisterServerAsync(string name, CancellationToken cancellationToken = default) => this.PostAsync(
            "v1/api/unregisterSdkDeviceAdapter",
            new { Name = name },
            cancellationToken
        );

        private static Task<string> GetContentAsync(HttpResponseMessage message, CancellationToken cancellationToken)
        {
            return message.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent
                ? message.Content.ReadAsStringAsync(cancellationToken)
                : throw new WebException($"Server returned {(int)message.StatusCode}:{message.StatusCode}.");
        }

        private string GetUri(string path) => $"http://{this.HostName}.local:{this.Port}/{path}";
    }
}
