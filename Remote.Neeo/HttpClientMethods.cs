using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Remote.Neeo.Json;

namespace Remote.Neeo;

internal static class HttpClientMethods
{
    public static readonly HttpClientHandler ClientHandler = new() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };

    public static async Task<TData> FetchAsync<TData>(this HttpClient httpClient, string uri, HttpMethod method, ByteArrayContent? body = default, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(method, uri) { Content = body };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new WebException($"Server returned {(int)response.StatusCode}:{response.StatusCode}.");
        }
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return (await JsonSerializer.DeserializeAsync<TData>(stream, JsonSerialization.Options, cancellationToken).ConfigureAwait(false))!;
    }
}