using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Api.Json;

namespace Neeo.Api;

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
    /// <typeparam name="TData">The type of data to deserialize from the response.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="cancellationToken">A cancellation token for the request.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TData> GetAsync<TData>(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously fetch data via a GET request to an endpoint on the Brain at the specified API
    /// <paramref name="path"/> and return the output of the specified <paramref name="transform"/>.
    /// </summary>
    /// <typeparam name="TData">The type of data to deserialize from the response.</typeparam>
    /// <typeparam name="TOutput">The output type of the transform.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="transform">The transformation to run on the data.</param>
    /// <param name="cancellationToken">A cancellation token for the request.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken = default);

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
    /// <typeparam name="TData">The type of data to deserialize from the response.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
    /// <param name="cancellationToken">A cancellation token for the request.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TData> PostAsync<TBody, TData>(string path, TBody body, CancellationToken cancellationToken = default);

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
    /// <param name="cancellationToken">A cancellation token for the request.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TOutput> PostAsync<TBody, TData, TOutput>(string path, TBody body, Func<TData, TOutput> transform, CancellationToken cancellationToken = default);
}

internal sealed class ApiClient : IApiClient, IDisposable
{
    private readonly HttpClient _httpClient = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    private readonly ILogger<ApiClient> _logger;
    private readonly string _uriPrefix;

    public ApiClient(SdkEnvironment environment, ILogger<ApiClient> logger) => (this._uriPrefix, this._logger) = ($"http://{environment.BrainEndPoint}", logger);

    public void Dispose() => this._httpClient.Dispose();

    public Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken) => this.GetAsync<JsonElement>(path, cancellationToken);

    public Task<TData> GetAsync<TData>(string path, CancellationToken cancellationToken) => this.GetAsync(path, (TData data) => data, cancellationToken);

    public Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken) => this.FetchAsync(
        path,
        HttpMethod.Get,
        null,
        transform,
        cancellationToken
    );

    public Task<JsonElement> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) => this.PostAsync<TBody, JsonElement>(path, body, cancellationToken);

    public Task<TData> PostAsync<TBody, TData>(string path, TBody body, CancellationToken cancellationToken) => this.PostAsync(path, body, (TData data) => data, cancellationToken);

    public Task<TOutput> PostAsync<TBody, TData, TOutput>(string path, TBody body, Func<TData, TOutput> transform, CancellationToken cancellationToken) => this.FetchAsync(
        path,
        HttpMethod.Post,
        new(JsonSerializer.SerializeToUtf8Bytes(body, JsonSerialization.Options)) { Headers = { ContentType = new("application/json") } },
        transform,
        cancellationToken
    );

    private async Task<TOutput> FetchAsync<TData, TOutput>(string path, HttpMethod method, ByteArrayContent? body, Func<TData, TOutput> transform, CancellationToken cancellationToken = default)
    {
        string uri = this._uriPrefix + path;
        this._logger.LogInformation("Making {method} request to {uri}...", method.Method, uri);
        TData data = await this._httpClient.FetchAsync<TData>(uri, method, body, cancellationToken).ConfigureAwait(false);
        return transform(data);
    }
}
