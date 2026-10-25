using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Extension methods for <see cref="HttpClient"/> for use with JSON.
/// </summary>
public static class HttpClientMethods
{
    private static readonly MediaTypeWithQualityHeaderValue _applicationJson = new("application/json");

    /// <summary>
    /// Makes a GET request to the server and returns the response data deserialized from JSON to an instance <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> instance.</param>
    /// <param name="uri">The request URI.</param>
    /// <param name="typeInfo">The source-generated JSON metadata used to deserialize the response.</param>
    /// <param name="configureRequest">Optional callback that can be used to further configure the request, such as adding request headers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the JSON response.</returns>
    public static async Task<TValue> GetAsync<TValue>(
        this HttpClient client,
        Uri uri,
        JsonTypeInfo<TValue> typeInfo,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    ) where TValue : notnull
    {
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(uri, HttpMethod.Get, default, configureRequest);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.DeserializeAsync(typeInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Makes a HEAD request to the server and returns the HTTP status code of the response.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance.</param>
    /// <param name="uri">The request URI.</param>
    /// <param name="configureRequest">Optional callback that can be used to further configure the request, such as adding request headers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The http status code returned by the server.</returns>
    public static async Task<HttpStatusCode> HeadAsync(
        this HttpClient client,
        Uri uri,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    )
    {
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(uri, HttpMethod.Head, default, configureRequest);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.StatusCode;
    }

    /// <summary>
    /// Makes a POST request to the server and returns the response data deserialized from JSON to an instance <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> instance.</param>
    /// <param name="uri">The request URI.</param>
    /// <param name="typeInfo">The source-generated JSON metadata used to deserialize the response.</param>
    /// <param name="configureRequest">Optional callback that can be used to further configure the request, such as adding request headers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the JSON response.</returns>
    public static async Task<TValue> PostAsync<TValue>(
        this HttpClient client,
        Uri uri,
        JsonTypeInfo<TValue> typeInfo,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    ) where TValue : notnull
    {
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(uri, HttpMethod.Post, default, configureRequest);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.DeserializeAsync(typeInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Makes a POST request to the server and returns the response data deserialized from JSON to an instance <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TBody">The type serialize into the request body as JSON.</typeparam>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> instance.</param>
    /// <param name="uri">The request URI.</param>
    /// <param name="body">Optional object to be serialized into the request body as JSON, omitted if <c>null</c>.</param>
    /// <param name="bodyTypeInfo">The source-generated JSON metadata used to serialize the request body.</param>
    /// <param name="typeInfo">The source-generated JSON metadata used to deserialize the response.</param>
    /// <param name="configureRequest">Optional callback that can be used to further configure the request, such as adding request headers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A <typeparamref name="TValue"/> representation of the JSON response.</returns>
    public static async Task<TValue> PostAsync<TBody, TValue>(
        this HttpClient client,
        Uri uri,
        TBody? body,
        JsonTypeInfo<TBody> bodyTypeInfo,
        JsonTypeInfo<TValue> typeInfo,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    ) where TBody : notnull where TValue : notnull
    {
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(
            uri,
            HttpMethod.Post,
            body == null ? null : JsonContent.Create(body, bodyTypeInfo),
            configureRequest
        );
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.DeserializeAsync(typeInfo, cancellationToken).ConfigureAwait(false);
    }

    private static HttpRequestMessage CreateRequest(
        Uri uri,
        HttpMethod method,
        HttpContent? content,
        Action<HttpRequestMessage>? configureRequest = null
    )
    {
        HttpRequestMessage request = new()
        {
            RequestUri = uri,
            Method = method,
            Headers = { Accept = { HttpClientMethods._applicationJson } },
            Content = content,
        };
        configureRequest?.Invoke(request);
        return request;
    }

    private static async Task<TValue> DeserializeAsync<TValue>(
        this HttpResponseMessage response,
        JsonTypeInfo<TValue> typeInfo,
        CancellationToken cancellationToken = default
    ) where TValue : notnull
    {
        if (!response.IsSuccessStatusCode)
        {
            string contents = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Server returned status {(int)response.StatusCode} ({Enum.GetName(response.StatusCode)}). {contents}",
                inner: null,
                statusCode: response.StatusCode
            );
        }
        return (await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false))!;
    }
}
