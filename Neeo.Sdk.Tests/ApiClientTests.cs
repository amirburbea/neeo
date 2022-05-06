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
    private readonly Mock<HttpMessageHandler> _mockMessageHandler;

    public ApiClientTests()
    {
        Mock<IBrainInfo> mockBrain = new();
        mockBrain.SetupGet(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        this._mockMessageHandler = new();
        this._client = new(mockBrain.Object, this._mockMessageHandler.Object, NullLogger<ApiClient>.Instance);
        /*Default implementation overriden in some tests.*/
        this.SetupResponse(_ => new TaskCompletionSource<object>().Task);
    }

    private interface IMessageHandlerMockedMethods
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Ensure SendAsync was called and returns the request message.
    /// </summary>
    private Func<HttpRequestMessage> VerifySend { get; set; } = () => throw new("Setup not completed.");

    public void Dispose() => this._client.Dispose();

    [Theory]
    [InlineData("abcde", 5)]
    [InlineData("", 0)]
    [InlineData("1234", 4)]
    public async Task GetAsyncTransformsResponse(string response, int expectedOutput)
    {
        this.SetupResponse(_ => Task.FromResult(response));
        int output = await this._client.GetAsync("/", static (string text) => text.Length);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void GetAsyncUsesHttpMethodGet()
    {
        _ = this._client.GetAsync("/", Identity<object>.Function);
        var request = this.VerifySend();
        Assert.Equal("GET", request.Method.Method);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    public void PathsAreConcatenatedCorrectly(string path)
    {
        _ = this._client.GetAsync(path, Identity<object>.Function);
        var request = this.VerifySend();
        Assert.Equal($"http://127.0.0.1:1234{path}", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task PathThrowsIfDoesNotStartWithForwardSlash()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => this._client.GetAsync("path_without_preceding_slash", Identity<object>.Function));
        Assert.Equal("path", exception.ParamName);
    }

    [Theory]
    [InlineData("abcde", 5)]
    [InlineData("", 0)]
    [InlineData("1234", 4)]
    public async Task PostAsyncTransformsResponse(string response, int expectedOutput)
    {
        this.SetupResponse(_ => Task.FromResult(response));
        int output = await this._client.PostAsync("/", string.Empty, static (string text) => text.Length);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void PostAsyncUsesHttpMethodPostWithCorrectBody()
    {
        var body = new { A = "123" };
        _ = this._client.PostAsync("/", body, Identity<object>.Function);
        var request = this.VerifySend();
        Assert.Equal("POST", request.Method.Method);
        Assert.NotNull(request.Content);
        using StreamReader reader = new(request.Content!.ReadAsStream());
        Assert.Equal(JsonSerializer.Serialize(body, JsonSerialization.Options), reader.ReadToEnd());
    }

    private void SetupResponse<T>(Func<HttpRequestMessage, Task<T>> callback)
    {
        List<HttpRequestMessage> captured = new();
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.SendAsync(Capture.In(captured), It.IsAny<CancellationToken>()))
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) => new() { Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(await callback(request).ConfigureAwait(false), JsonSerialization.Options)) });
        this.VerifySend = () =>
        {
            this._mockMessageHandler
                .Protected()
                .As<IMessageHandlerMockedMethods>()
                .Verify(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());
            return captured.Single();
        };
    }

    private static class Identity<T>
    {
        public static Func<T, T> Function = x => x;
    }
}