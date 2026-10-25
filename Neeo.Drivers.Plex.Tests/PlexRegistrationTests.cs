using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices.Setup;
using Xunit;

namespace Neeo.Drivers.Plex.Tests;

public sealed class PlexRegistrationTests
{
    private const string Token = "plex-secret-token";

    [Fact]
    public async Task QueryIsRegistered_is_false_without_a_token_and_does_not_contact_plex()
    {
        FakeTokenStore tokens = new();
        StubHttpMessageHandler handler = new((_, _) => throw new InvalidOperationException("should not be called"));
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, handler);

        bool registered = await provider.QueryIsRegisteredAsync(TestContext.Current.CancellationToken);

        Assert.False(registered);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task QueryIsRegistered_is_true_and_keeps_the_token_when_plex_accepts_it()
    {
        FakeTokenStore tokens = new(Token);
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        bool registered = await provider.QueryIsRegisteredAsync(TestContext.Current.CancellationToken);

        Assert.True(registered);
        Assert.Equal(Token, tokens.AuthToken);
    }

    [Fact]
    public async Task QueryIsRegistered_is_false_and_clears_the_token_when_plex_rejects_it()
    {
        FakeTokenStore tokens = new(Token);
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

        bool registered = await provider.QueryIsRegisteredAsync(TestContext.Current.CancellationToken);

        Assert.False(registered);
        Assert.Null(tokens.AuthToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task QueryIsRegistered_keeps_the_token_when_plex_has_a_server_error(HttpStatusCode status)
    {
        FakeTokenStore tokens = new(Token);
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, new((_, _) => Task.FromResult(new HttpResponseMessage(status))));

        bool registered = await provider.QueryIsRegisteredAsync(TestContext.Current.CancellationToken);

        Assert.True(registered);
        Assert.Equal(Token, tokens.AuthToken);
    }

    [Theory]
    [MemberData(nameof(NetworkFailures))]
    public async Task QueryIsRegistered_keeps_the_token_when_plex_cannot_be_reached(Exception failure)
    {
        FakeTokenStore tokens = new(Token);
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, new((_, _) => throw failure));

        bool registered = await provider.QueryIsRegisteredAsync(TestContext.Current.CancellationToken);

        Assert.True(registered);
        Assert.Equal(Token, tokens.AuthToken);
    }

    [Fact]
    public async Task QueryIsRegistered_keeps_the_token_and_propagates_when_cancelled()
    {
        FakeTokenStore tokens = new(Token);
        using CancellationTokenSource cts = new();
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, new((_, token) =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.QueryIsRegisteredAsync(cts.Token));

        Assert.Equal(Token, tokens.AuthToken);
    }

    [Fact]
    public async Task Register_fails_with_a_message_when_plex_rejects_the_credentials()
    {
        FakeTokenStore tokens = new();
        using PlexMediaPlayerDeviceProvider provider = PlexRegistrationTests.CreateProvider(tokens, new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));

        RegistrationResult result = await provider.RegisterAsync("user", "wrong-password", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.Error);
        Assert.Null(tokens.AuthToken);
    }

    public static TheoryData<Exception> NetworkFailures() => new()
    {
        new HttpRequestException("Name or service not known"),
        new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"),
        new System.Net.Sockets.SocketException(),
    };

    private static PlexMediaPlayerDeviceProvider CreateProvider(IPlexTokenStore tokens, StubHttpMessageHandler handler)
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));
        Mock<IHostApplicationLifetime> lifetime = new();
        lifetime.SetupGet(l => l.ApplicationStarted).Returns(new CancellationToken(true));
        return new(factory.Object, Mock.Of<IPlexServerManager>(), tokens, lifetime.Object, NullLogger<PlexMediaPlayerDeviceProvider>.Instance);
    }
}
