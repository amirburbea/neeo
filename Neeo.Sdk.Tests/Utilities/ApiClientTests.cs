using System;
using System.IO;
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
    private readonly ApiClient _apiClient;
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _mockMessageHandler = new(MockBehavior.Strict);

    public ApiClientTests()
    {
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        this._apiClient = new(mockBrain.Object, this._httpClient = new(this._mockMessageHandler.Object), NullLogger<ApiClient>.Instance);
        // Required in strict mocks.
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.Dispose(true));
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
        _ = this.SetupJsonResponse(data);

        Assert.Equal(data.Length, await this._apiClient.GetAsync("/", (string text) => text.Length));
    }

    [Fact]
    public async Task GetAsync_should_use_HTTP_GET()
    {
        Task<RequestData> task = this.SetupJsonResponse(new object());

        await this._apiClient.GetAsync("/", Identity<object>.Projection);

        RequestData data = await task;
        Assert.Equal(HttpMethod.Get, data.Request.Method);
        Assert.Null(data.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcde")]
    [InlineData("1234")]
    public async Task PostAsync_should_transform_response_body(string data)
    {
        _ = this.SetupJsonResponse(data);

        Assert.Equal(data.Length, await this._apiClient.PostAsync("/", body: new object(), static (string text) => text.Length));
    }

    [Fact]
    public async Task PostAsync_should_use_HTTP_POST_and_serialize_bodyAsync()
    {
        Task<RequestData> task = this.SetupJsonResponse(new object());

        var body = new { A = "123" };
        _ = this._apiClient.PostAsync("/", body, Identity<object>.Projection);

        RequestData data = await task;
        Assert.Equal(HttpMethod.Post, data.Request.Method);
        Assert.NotNull(data.Body);
        Assert.Equal(JsonSerializer.Serialize(body, JsonSerialization.Options), data.Body);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    public async void Requests_should_concatenate_paths_correctly(string path)
    {
        Task<RequestData> task = this.SetupJsonResponse(new object());

        _ = this._apiClient.GetAsync(path, Identity<object>.Projection);

        RequestData data = await task;
        Assert.Equal($"http://127.0.0.1:1234{path}", data.Request.RequestUri!.ToString());
    }

    [Fact]
    public Task Requests_should_throw_on_path_without_preceding_slash() => Assert.ThrowsAsync<ArgumentException>(
        () => this._apiClient.GetAsync("path_without_preceding_slash", Identity<object>.Projection)
    );

    private Task<RequestData> SetupJsonResponse<T>(T data)
    {
        TaskCompletionSource<RequestData> source = new();
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.SendAsync(
                Capture.With(new CaptureMatch<HttpRequestMessage>(request => source.SetResult(new(request, GetBody(request))))), 
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(value: new() { Content = new StringContent(JsonSerializer.Serialize(data, JsonSerialization.Options)) });
        return source.Task;
    }

    static string? GetBody(HttpRequestMessage request)
    {
        if (request.Content is not { } content)
        {
            return null;
        }
        using StreamReader reader = new(content.ReadAsStream(default));
        return reader.ReadToEnd();
    }

    private readonly record struct RequestData(HttpRequestMessage Request,  string? Body);
}