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
    /// <paramref name="path"/>.
    /// </summary>
    /// <typeparam name="TData">The type of data to deserialize from the response.</typeparam>
    /// <param name="path">The API path on the NEEO Brain.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<TData> GetAsync<TData>(string path, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Asynchronously probes the NEEO Brain to verify it is reachable and responsive.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see langword="true"/> if the Brain responded successfully; otherwise <see langword="false"/>.</returns>
    Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
}

internal sealed class ApiClient(
    IBrain brain,
    IHttpClientFactory httpClientFactory,
    ILogger<ApiClient> logger
) : IApiClient
{
    private readonly Uri _baseUri = new($"http://{brain.ServiceEndPoint}");
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(ApiClient));

    public Task<TData> GetAsync<TData>(string path, CancellationToken cancellationToken = default)
        where TData : notnull
    {
        UriBuilder builder = new(this._baseUri) {Path = path};
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Making GET request to {uri}...", builder.Uri);
        }
        return this._httpClient.GetAsync(builder.Uri, AppJsonSerializerContext.TypeInfo<TData>(), cancellationToken: cancellationToken);
    }

    public async Task<bool> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken = default)
        where TBody : notnull
    {
        UriBuilder builder = new(this._baseUri) { Path = path };
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Making POST request to {uri}...", builder.Uri);
        }
        SuccessResponse response = await this._httpClient
            .PostAsync(builder.Uri, body, AppJsonSerializerContext.TypeInfo<TBody>(), AppJsonSerializerContext.Default.SuccessResponse, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Success;
    }

    public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            UriBuilder builder = new(this._baseUri) { Path = BrainUrlPaths.SystemInfo };
            using HttpResponseMessage response = await this._httpClient.GetAsync(
                builder.Uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            ).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
