using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests;

public sealed class SdkEnvironmentTests
{
    [Fact]
    public async Task StopAsync_stops_server_and_host_and_disposes_host()
    {
        Mock<IServer> mockServer = new(MockBehavior.Strict);
        mockServer.Setup(s => s.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IHost> mockHost = new(MockBehavior.Strict);
        mockHost.Setup(h => h.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockHost.Setup(h => h.Dispose());

        SdkEnvironment environment = new((SdkAdapterName)"test", Mock.Of<IBrain>(), mockServer.Object, mockHost.Object);

        await environment.StopAsync(TestContext.Current.CancellationToken);

        mockServer.Verify(s => s.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockHost.Verify(h => h.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockHost.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_disposes_host_even_if_server_stop_throws()
    {
        Mock<IServer> mockServer = new(MockBehavior.Strict);
        mockServer.Setup(s => s.StopAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("server stop error"));
        Mock<IHost> mockHost = new(MockBehavior.Strict);
        mockHost.Setup(h => h.Dispose());

        SdkEnvironment environment = new((SdkAdapterName)"test", Mock.Of<IBrain>(), mockServer.Object, mockHost.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => environment.StopAsync(TestContext.Current.CancellationToken));

        mockHost.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_disposes_host_even_if_host_stop_throws()
    {
        Mock<IServer> mockServer = new(MockBehavior.Strict);
        mockServer.Setup(s => s.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IHost> mockHost = new(MockBehavior.Strict);
        mockHost.Setup(h => h.StopAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("host stop error"));
        mockHost.Setup(h => h.Dispose());

        SdkEnvironment environment = new((SdkAdapterName)"test", Mock.Of<IBrain>(), mockServer.Object, mockHost.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => environment.StopAsync(TestContext.Current.CancellationToken));

        mockHost.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact]
    public async Task StartSdkAsync_disposes_host_when_startup_fails()
    {
        TestDisposableService.WasDisposed = false;
        TestServiceConfiguration config = new();

        // When a hosted service fails on startup, StartSdkAsync throws and must dispose the host.
        await Assert.ThrowsAnyAsync<Exception>(() => Server.StartSdkAsync(
            new Brain(IPAddress.Loopback),
            _ => [],
            "test-adapter",
            IPAddress.Loopback,
            port: 0,
            serviceConfigurations: [config],
            cancellationToken: TestContext.Current.CancellationToken
        ));

        Assert.True(TestDisposableService.WasDisposed, "Host must be disposed when startup fails so allocated services do not leak.");
    }

    private sealed class TestDisposableService : IDisposable
    {
        public static bool WasDisposed { get; set; }

        public void Dispose() => WasDisposed = true;
    }

    private sealed class FaultingHostedService(TestDisposableService service) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => throw new InvalidOperationException($"Startup failure simulation with {service}.");

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestServiceConfiguration : IServiceConfiguration
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestDisposableService>();
            services.AddHostedService<FaultingHostedService>();
        }
    }
}
