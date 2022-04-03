using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk;

/// <summary>
/// Brain REST API client.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API
    /// <paramref name="path"/> and return the output of the specified <paramref name="transform"/>.
    /// </summary>
    /// <typeparam name="TData">The type of data to deserialize from the response.</typeparam>
    /// <typeparam name="TOutput">The output type of the transform.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="transform">The transformation to run on the data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API
    /// <paramref name="path"/> and return the output of the specified <paramref name="transform"/>.
    /// </summary>
    /// <typeparam name="TBody">The type of the body.</typeparam>
    /// <typeparam name="TData">The type of data to deserialize from the response.</typeparam>
    /// <typeparam name="TOutput">The output type of the transform.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
    /// <param name="transform">The transformation to run on the data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TOutput> PostAsync<TBody, TData, TOutput>(string path, TBody body, Func<TData, TOutput> transform, CancellationToken cancellationToken = default);
}

internal sealed class ApiClient : IApiClient, IDisposable
{
    private static readonly MediaTypeHeaderValue _jsonContentType = new("application/json");

    private readonly HttpClient _httpClient = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    private readonly ILogger<ApiClient> _logger;
    private readonly string _uriPrefix;

    public ApiClient(Brain brain, ILogger<ApiClient> logger) => (this._uriPrefix, this._logger) = ($"http://{brain.ServiceEndPoint}", logger);

    public void Dispose() => this._httpClient.Dispose();

    public Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken) => this.FetchAsync(
        path,
        HttpMethod.Get,
        default,
        transform,
        cancellationToken
    );

    public async Task<TOutput> PostAsync<TBody, TData, TOutput>(string path, TBody body, Func<TData, TOutput> transform, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new();
        await JsonSerializer.SerializeAsync(stream, body, JsonSerialization.Options, cancellationToken).ConfigureAwait(false);
        stream.Seek(0L, SeekOrigin.Begin);
        using StreamContent content = new(stream) { Headers = { ContentType = ApiClient._jsonContentType } };
        return await this.FetchAsync(path, HttpMethod.Post, content, transform, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TOutput> FetchAsync<TData, TOutput>(string path, HttpMethod method, HttpContent? content, Func<TData, TOutput> transform, CancellationToken cancellationToken)
    {
        string uri = this._uriPrefix + path;
        this._logger.LogInformation("Making {method} request to {uri}...", method.Method, uri);
        using HttpRequestMessage request = new(method, uri) { Content = content };
        using HttpResponseMessage response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return transform((await JsonSerializer.DeserializeAsync<TData>(stream, JsonSerialization.Options, cancellationToken).ConfigureAwait(false))!);
        }
        using StreamReader reader = new(stream);
        throw new WebException($"Server returned status {(int)response.StatusCode} ({Enum.GetName(response.StatusCode)}). ${reader.ReadToEnd()}");
    }
}