using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Remote.Neeo.Json;

namespace Remote.Neeo
{
    partial record Brain
    {
        /// <summary>
        /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            return this.GetAsync<JsonElement>(path, cancellationToken);
        }

        /// <summary>
        /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of data to deserialize from the response.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<TResult> GetAsync<TResult>(string path, CancellationToken cancellationToken = default)
        {
            return this.FetchAsync<TResult>(
                path,
                HttpMethod.Get,
                token: cancellationToken
            );
        }

        /// <summary>
        /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TBody">The type of the body.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
        /// <param name="cancellationToken">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<JsonElement> PostAsync<TBody>(
            string path,
            TBody body,
            CancellationToken cancellationToken = default
        )
        {
            return this.PostAsync<TBody, JsonElement>(path, body, cancellationToken);
        }

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
        public Task<TResult> PostAsync<TBody, TResult>(
            string path,
            TBody body,
            CancellationToken cancellationToken = default
        )
        {
            return this.FetchAsync<TResult>(
                path,
                HttpMethod.Post,
                new(JsonSerializer.SerializeToUtf8Bytes(body, JsonSerialization.Options))
                {
                    Headers = { ContentType = new("application/json") }
                },
                cancellationToken
            );
        }

        /// <summary>
        /// Asynchronously fetch data via a request to a REST endpoint on the Brain at the specified API
        /// <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of data to deserialize from the response.</typeparam>
        /// <param name="path">The API path on the NEEO Brain.</param>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="content">The content of the request.</param>
        /// <param name="token">A cancellation token for the request.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<TResult> FetchAsync<TResult>(
            string path,
            HttpMethod method,
            ByteArrayContent? content = default,
            CancellationToken token = default
        )
        {
            using HttpClientHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            using HttpClient client = new(handler);
            using HttpResponseMessage message = await client.SendAsync(
                new(method, this.GetUri(path)) { Content = content },
                token
            ).ConfigureAwait(false);
            if (message.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException($"Server returned {(int)message.StatusCode}:{message.StatusCode}.");
            }
            using Stream stream = await message.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            return (await JsonSerializer.DeserializeAsync<TResult>(stream, JsonSerialization.Options, token)
                .ConfigureAwait(false))!;
        }

        internal Task<SuccessResult> RegisterServerAsync(
            string name,
            string baseUrl,
            CancellationToken cancellationToken
        )
        {
            return this.PostAsync<object, SuccessResult>(
                "api/registerSdkDeviceAdapter",
                new { Name = name, BaseUrl = baseUrl },
                cancellationToken
            );
        }

        internal Task<SuccessResult> UnregisterServerAsync(string name, CancellationToken cancellationToken)
        {
            return this.PostAsync<object, SuccessResult>(
                "api/unregisterSdkDeviceAdapter",
                new { Name = name },
                cancellationToken
            );
        }

        private string GetUri(string path) => $"http://{this.IPAddress}:{this.Port}/v1/{path}";
    }
}