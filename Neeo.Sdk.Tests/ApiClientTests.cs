using System;
using System.Collections.Generic;
using System.IO;
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

namespace Neeo.Sdk.Tests;

public sealed class ApiClientTests : IDisposable
{
    private readonly ApiClient _client;
    private readonly Mock<HttpMessageHandler> _mockMessageHandler = new(MockBehavior.Strict);

    public ApiClientTests()
    {
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        this._client = new(mockBrain.Object, this._mockMessageHandler.Object, NullLogger<ApiClient>.Instance);
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

    public void Dispose() => this._client.Dispose();

    [Theory]
    [InlineData("abcde", 5)]
    [InlineData("", 0)]
    [InlineData("1234", 4)]
    public async Task GetAsync_should_transform_response_body(string response, int expectedOutput)
    {
        this.SetupResponse(response);

        int output = await this._client.GetAsync("/", static (string text) => text.Length);

        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void GetAsync_should_use_HTTP_GET()
    {
        var request = this.SetupResponse(new object());

        _ = this._client.GetAsync("/", Mock.Of<Func<object, object>>());

        Assert.Equal("GET", request.Value.Method.Method);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abcde", "edcba")]
    [InlineData("1234", "4321")]
    public async Task PostAsync_should_transform_response_body(string response, string expectedOutput)
    {
        this.SetupResponse(response);

        string output = await this._client.PostAsync("/", string.Empty, static (string text) => new string(text.Reverse().ToArray()));

        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void PostAsync_should_use_HTTP_POST_and_serialize_body()
    {
        var body = new { A = "123" };
        var request = this.SetupResponse(new TaskCompletionSource<object>().Task);

        _ = this._client.PostAsync("/", body, Mock.Of<Func<object, object>>());

        Assert.Equal("POST", request.Value.Method.Method);
        Assert.NotNull(request.Value.Content);
        using StreamReader reader = new(request.Value.Content!.ReadAsStream());
        Assert.Equal(JsonSerializer.Serialize(body, JsonSerialization.Options), reader.ReadToEnd());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    public void Requests_should_concatenate_paths_correctly(string path)
    {
        var request = this.SetupResponse(new object());

        _ = this._client.GetAsync(path, Mock.Of<Func<object, object>>());

        Assert.Equal($"http://127.0.0.1:1234{path}", request.Value.RequestUri!.ToString());
    }

    [Fact]
    public Task Requests_should_throw_on_path_without_preceding_slash() => Assert.ThrowsAsync<ArgumentException>(
        () => this._client.GetAsync("path_without_preceding_slash", Mock.Of<Func<object, object>>())
    );

    private Lazy<HttpRequestMessage> SetupResponse<T>(T value) => this.SetupResponse(Task.FromResult(value));

    private Lazy<HttpRequestMessage> SetupResponse<T>(Task<T> task)
    {
        List<HttpRequestMessage> captured = new();
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.SendAsync(Capture.In(captured), It.IsAny<CancellationToken>()))
            .Returns(async (HttpRequestMessage _, CancellationToken _) => new()
            {
                Content = new StringContent(JsonSerializer.Serialize(await task, JsonSerialization.Options))
            });
        return new(() => captured.Single());
    }
}