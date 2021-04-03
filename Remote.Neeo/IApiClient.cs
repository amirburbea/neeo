using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remote.Neeo.Json;

namespace Remote.Neeo
{
    /// <summary>
    /// Brain REST API client.
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of data to deserialize from the response.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task<TResult> GetAsync<TResult>(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TBody">The type of the body.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task<JsonElement> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TBody">The type of the body.</typeparam>
        /// <typeparam name="TResult">The type of data to deserialize from the response.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task<TResult> PostAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken = default);
    }

    internal sealed class ApiClient : IApiClient
    {
        private readonly ILogger<ApiClient> _logger;
        private readonly string _uriPrefix;

        public ApiClient(Brain brain, ILogger<ApiClient> logger)
        {
            (this._uriPrefix, this._logger) = ($"http://{brain.ServiceEndPoint}", logger);
        }

        public Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken) => this.GetAsync<JsonElement>(path, cancellationToken);

        public Task<TResult> GetAsync<TResult>(string path, CancellationToken cancellationToken) => this.FetchAsync<TResult>(path, HttpMethod.Get, token: cancellationToken);

        public Task<JsonElement> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) => this.PostAsync<TBody, JsonElement>(path, body, cancellationToken);

        public Task<TResult> PostAsync<TBody, TResult>(string path, TBody body, CancellationToken cancellationToken) => this.FetchAsync<TResult>(
            path, HttpMethod.Post,
            new(JsonSerializer.SerializeToUtf8Bytes(body, JsonSerialization.Options)) { Headers = { ContentType = new("application/json") } },
            cancellationToken
        );

        private async Task<TResult> FetchAsync<TResult>(string path, HttpMethod method, ByteArrayContent? content = default, CancellationToken token = default)
        {
            string uri = this._uriPrefix + path;
            this._logger.LogInformation("Making {method} request to {uri}...", method.Method, uri);
            using HttpClientHandler handler = new() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            using HttpClient client = new(handler);
            HttpRequestMessage request = new(method, uri) { Content = content };
            using HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException($"Server returned {(int)response.StatusCode}:{response.StatusCode}.");
            }
            using Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            return (await JsonSerializer.DeserializeAsync<TResult>(stream, JsonSerialization.Options, token).ConfigureAwait(false))!;
        }
    }
}