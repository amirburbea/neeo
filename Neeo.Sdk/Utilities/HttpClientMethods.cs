using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Extension methods for <see cref="System.Net.Http.HttpClient"/>.
/// </summary>
public static class HttpClientMethods
{
    private static readonly MediaTypeWithQualityHeaderValue _applicationJson = new("application/json");

    public static async Task<TData> GetAsync<TData>(
        this HttpClient client,
        Uri uri,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    ) where TData : notnull
    {
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(uri, HttpMethod.Get, default, configureRequest);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.DeserializeAsync<TData>(cancellationToken).ConfigureAwait(false);
    }

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

    public static async Task<TData> PostAsync<TData>(
        this HttpClient client,
        Uri uri,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    ) where TData : notnull
    {
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(uri, HttpMethod.Post, null, configureRequest);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.DeserializeAsync<TData>(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<TData> PostAsync<TBody, TData>(
        this HttpClient client,
        Uri uri,
        TBody body,
        Action<HttpRequestMessage>? configureRequest = null,
        CancellationToken cancellationToken = default
    ) where TBody : notnull where TData : notnull
    {
        using MemoryStream stream = new();
        await JsonSerializer.SerializeAsync(stream, body, JsonSerialization.Options, cancellationToken).ConfigureAwait(false);
        stream.Seek(0L, SeekOrigin.Begin);
        using StreamContent content = new(stream) { Headers = { ContentType = HttpClientMethods._applicationJson } };
        using HttpRequestMessage request = HttpClientMethods.CreateRequest(uri, HttpMethod.Post, content, configureRequest);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.DeserializeAsync<TData>(cancellationToken).ConfigureAwait(false);
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

    private static async Task<TData> DeserializeAsync<TData>(this HttpResponseMessage response, CancellationToken cancellationToken = default)
        where TData : notnull
    {
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            return (await JsonSerializer.DeserializeAsync<TData>(stream, JsonSerialization.Options, cancellationToken).ConfigureAwait(false))!;
        }
        using StreamReader reader = new(stream);
        throw new WebException($"Server returned status {(int)response.StatusCode} ({Enum.GetName(response.StatusCode)}). ${reader.ReadToEnd()}");
    }
}
