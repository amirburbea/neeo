using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Utilities;

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
    Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken = default)
        where TData : notnull;

    /// <summary>
    /// Asynchronously fetch data via a POST request to an endpoint on the Brain at the specified API
    /// <paramref name="path"/> and return the a value indicating success.
    ///
    /// All NEEO Brain APIs returns a simple success response.
    /// </summary>
    /// <typeparam name="TBody">The type of the body.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="body">An object to serialize into JSON to be used as the body of the request.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken = default)
        where TBody : notnull;
}

internal sealed class ApiClient(IBrain brain, IHttpClientFactory httpClientFactory, ILogger<ApiClient> logger) : IApiClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(ApiClient));
    private readonly string _uriPrefix = $"http://{brain.ServiceEndPoint}";

    public async Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken = default)
        where TData : notnull
    {
        Uri uri = this.GetUri(path);
        logger.LogInformation("Making GET request to {uri}...", uri);
        TData data = await this._httpClient.GetAsync<TData>(uri, cancellationToken: cancellationToken).ConfigureAwait(false);
        return transform(data);
    }

    public async Task<bool> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken = default)
        where TBody : notnull
    {
        Uri uri = this.GetUri(path);
        logger.LogInformation("Making POST request to {uri}...", uri);
        SuccessResponse response = await this._httpClient.PostAsync<TBody, SuccessResponse>(uri, body, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Success;
    }

    private Uri GetUri(string path) => path.StartsWith('/')
        ? new(this._uriPrefix + path)
        : throw new ArgumentException("Path must start with a forward slash (\"/\").", nameof(path));
}
