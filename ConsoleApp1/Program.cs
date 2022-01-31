using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Neeo.Sdk;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Json;

namespace Foo
{
    class Program
    {
        async static Task Main()
        {
            IApiClient client = new ApiClient("192.168.253.163", 6336);
            ListParameters parameters = new(string.Empty);

            var data = await client.PostAsync("/device/apt-a2e26419d140e68317c2049504fda07273cc6c9c/BROWSE_DIRECTORY/default",parameters,(JsonElement e)=>e.ToString());




        }
    }
}

internal sealed class ApiClient : IApiClient, IDisposable
{
    private static readonly MediaTypeHeaderValue _jsonContentType = new("application/json");

    private readonly HttpClient _httpClient = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    
    private readonly string _uriPrefix;

    public ApiClient(string ipAddress, int port) => this._uriPrefix=$"http://{ipAddress}:{port}";

    public void Dispose() => this._httpClient.Dispose();

    public Task<TOutput> GetAsync<TData, TOutput>(string path, Func<TData, TOutput> transform, CancellationToken cancellationToken) => this.FetchAsync(
        path,
        HttpMethod.Get,
        null,
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

    private async Task<TOutput> FetchAsync<TData, TOutput>(string path, HttpMethod method, HttpContent? content, Func<TData, TOutput> transform, CancellationToken cancellationToken = default)
    {
        string uri = this._uriPrefix + path;
        using HttpRequestMessage request = new(method, uri) { Content = content };
        using HttpResponseMessage response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new WebException($"Server returned status {(int)response.StatusCode}:{Enum.GetName(response.StatusCode)}.");
        }
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return transform((await JsonSerializer.DeserializeAsync<TData>(stream, JsonSerialization.Options, cancellationToken).ConfigureAwait(false))!);
    }
}
