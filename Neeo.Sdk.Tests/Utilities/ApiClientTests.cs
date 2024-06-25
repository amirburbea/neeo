using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class ApiClientTests : IDisposable
{
    private readonly ApiClient _client;
    private readonly HttpClient _httpClient;
    private readonly Mock<IHttpClientFactory> _mockClientFactory = new(MockBehavior.Strict);
    private readonly Mock<HttpMessageHandler> _mockMessageHandler = new(MockBehavior.Strict);

    public ApiClientTests()
    {
        // Required in strict mocks.
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.Dispose(true));
        this._httpClient = new(this._mockMessageHandler.Object);
        this._mockClientFactory.Setup((factory) => factory.CreateClient(nameof(ApiClient))).Returns(this._httpClient);
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        this._client = new(mockBrain.Object, this._mockClientFactory.Object, NullLogger<ApiClient>.Instance);
    }

    private interface IMessageHandlerMockedMethods
    {
        void Dispose(bool disposing);

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public void Dispose() => this._httpClient.Dispose();

    [Theory]
    [InlineData("abcde")]
    [InlineData("")]
    [InlineData("1234")]
    public async Task GetAsync_should_transform_response_body(string data)
    {
        this.SetupJsonResponse(data);

        Assert.Equal(data.Length, await this._client.GetAsync("/", static (string text) => text.Length));
    }

    [Fact]
    public async Task GetAsync_should_use_HTTP_GET()
    {
        var lazy = this.SetupJsonResponse(new object());

        await this._client.GetAsync("/", IdentityFunction.For<object>());

        var (request, requestBody) = lazy.Value;
        Assert.Equal("GET", request.Method.Method);
        Assert.Null(requestBody);
    }

    [Fact]
    public void PostAsync_should_use_HTTP_POST_and_serialize_body()
    {
        var body = new { A = "123" };
        var lazy = this.SetupJsonResponse(new SuccessResponse(true));

        _ = this._client.PostAsync("/", body);

        var (request, requestBody) = lazy.Value;
        Assert.Equal("POST", request.Method.Method);
        Assert.NotNull(requestBody);
        Assert.Equal(JsonSerializer.Serialize(body, JsonSerialization.WebOptions), requestBody);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    public void Requests_should_concatenate_paths_correctly(string path)
    {
        var lazy = this.SetupJsonResponse(new object());

        _ = this._client.GetAsync(path, IdentityFunction.For<object>());

        var (request, _) = lazy.Value;
        Assert.Equal($"http://127.0.0.1:1234{path}", request.RequestUri!.ToString());
    }

    [Fact]
    public Task Requests_should_throw_on_path_without_preceding_slash() => Assert.ThrowsAsync<ArgumentException>(
        () => this._client.GetAsync("path_without_preceding_slash", IdentityFunction.For<object>())
    );

    private Lazy<(HttpRequestMessage, string?)> SetupJsonResponse<T>(T data)
    {
        List<HttpRequestMessage> captured = [];
        string? requestBody = default;
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.SendAsync(Capture.In(captured), It.IsAny<CancellationToken>()))
            .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                if (request.Content is { } content)
                {
                    requestBody = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                }
                return new()
                {
                    Content = new StringContent(JsonSerializer.Serialize(data, JsonSerialization.WebOptions))
                };
            });
        return new(() => (captured.Single(), requestBody));
    }

    internal static class IdentityFunction
    {
        /// <summary>
        /// Gets an identity function for items of type <typeparamref name="TItem"/>.
        /// </summary>
        public static Func<TItem, TItem> For<TItem>() => Identity<TItem>.Function;

        private static class Identity<TItem>
        {
            public static readonly Func<TItem, TItem> Function = item => item;
        }
    }
}
