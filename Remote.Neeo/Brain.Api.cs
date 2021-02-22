using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Remote.Neeo
{
    partial record Brain
    {
        /// <summary>
        /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken = default) => this.GetAsync<JsonElement>(path, cancellationToken);

        /// <summary>
        /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of data to deserialize from the response.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<TResult> GetAsync<TResult>(string path, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            HttpResponseMessage message = await client.GetAsync(this.GetUri(path), cancellationToken).ConfigureAwait(false);
            return await Brain.GetResult<TResult>(message, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TBody">The type of the body.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<JsonElement> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken = default) => this.PostAsync<TBody, JsonElement>(path, body, cancellationToken);

        /// <summary>
        /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TBody">The type of the body.</typeparam>
        /// <typeparam name="TResult">The type of data to deserialize from the response.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<TResult> PostAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            HttpResponseMessage message = await client.PostAsync(
                this.GetUri(path),
                new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body, JsonSerialization.Options)) { Headers = { ContentType = new("application/json") } },
                cancellationToken
            ).ConfigureAwait(false);
            return await Brain.GetResult<TResult>(message, cancellationToken).ConfigureAwait(false);
        }

        internal Task<SuccessResult> RegisterServerAsync(string name, string baseUrl, CancellationToken cancellationToken = default) => this.PostAsync<object, SuccessResult>(
            "api/registerSdkDeviceAdapter",
            new { Name = name, BaseUrl = baseUrl },
            cancellationToken
        );

        internal Task<SuccessResult> UnregisterServerAsync(string name, CancellationToken cancellationToken = default) => this.PostAsync<object, SuccessResult>(
            "api/unregisterSdkDeviceAdapter",
            new { Name = name },
            cancellationToken
        );

        private static async Task<TResult> GetResult<TResult>(HttpResponseMessage message, CancellationToken cancellationToken)
        {
            if (message.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException($"Server returned {(int)message.StatusCode}:{message.StatusCode}.");
            }
            using Stream stream = await message.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return (await JsonSerializer.DeserializeAsync<TResult>(stream, JsonSerialization.Options, cancellationToken).ConfigureAwait(false))!;
        }

        private string GetUri(string path) => $"http://{this.IPAddress}:{this.Port}/v1/{path}";

    }
}