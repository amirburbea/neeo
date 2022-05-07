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
        Mock<IBrain> mockBrain = new();
        mockBrain.SetupGet(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        this._mockMessageHandler = new();
        this._client = new(mockBrain.Object, this._mockMessageHandler.Object, NullLogger<ApiClient>.Instance);
        // Default.
        this.SetupResponse(_ => Task.FromResult(new object()));
    }

    private interface IMessageHandlerMockedMethods
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public void Dispose() => this._client.Dispose();

    [Theory]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    public void Should_Concatenate_Paths_Correctly(string path)
    {
        var request = this.SetupResponse(_ => new TaskCompletionSource<object>().Task);
        _ = this._client.GetAsync(path, static (object obj) => obj);
        Assert.Equal($"http://127.0.0.1:1234{path}", request.Value.RequestUri!.ToString());
    }

    [Fact]
    public Task Should_Throw_On_Path_Without_Preceding_Slash() => Assert.ThrowsAsync<ArgumentException>(
        () => this._client.GetAsync("path_without_preceding_slash", static (object obj) => obj)
    );

    [Theory]
    [InlineData("abcde", 5)]
    [InlineData("", 0)]
    [InlineData("1234", 4)]
    public async Task Should_Transform_Response_Body_In_GetAsync(string response, int expectedOutput)
    {
        this.SetupResponse(_ => Task.FromResult(response));
        int output = await this._client.GetAsync("/", static (string text) => text.Length);
        Assert.Equal(expectedOutput, output);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abcde", "edcba")]
    [InlineData("1234", "4321")]
    public async Task Should_Transform_Response_Body_In_PostAsync(string response, string expectedOutput)
    {
        this.SetupResponse(_ => Task.FromResult(response));
        string output = await this._client.PostAsync("/", string.Empty, static (string text) => new string(text.Reverse().ToArray()));
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void Should_Use_Http_Get_In_GetAsync()
    {
        var request = this.SetupResponse(_ => new TaskCompletionSource<object>().Task);
        _ = this._client.GetAsync("/", static (object obj) => obj);
        Assert.Equal("GET", request.Value.Method.Method);
    }

    [Fact]
    public void Should_Use_Http_Post_And_Serialize_Body_In_PostAsync()
    {
        var body = new { A = "123" };
        var request = this.SetupResponse(_ => new TaskCompletionSource<object>().Task);
        _ = this._client.PostAsync("/", body, static (object obj) => obj);
        Assert.Equal("POST", request.Value.Method.Method);
        Assert.NotNull(request.Value.Content);
        using StreamReader reader = new(request.Value.Content!.ReadAsStream());
        Assert.Equal(JsonSerializer.Serialize(body, JsonSerialization.Options), reader.ReadToEnd());
    }

    private Lazy<HttpRequestMessage> SetupResponse<T>(Func<HttpRequestMessage, Task<T>> callback)
    {
        List<HttpRequestMessage> captured = new();
        this._mockMessageHandler
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.SendAsync(Capture.In(captured), It.IsAny<CancellationToken>()))
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) => new() { Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(await callback(request).ConfigureAwait(false), JsonSerialization.Options)) });
        return new(() => captured.Single());
    }
}