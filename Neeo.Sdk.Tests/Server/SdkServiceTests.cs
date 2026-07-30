using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Server;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Server;

public sealed class SdkServiceTests
{
    [Fact]
    public async Task ExecuteAsync_starts_server_with_provider_types_and_service_configurations_using_configured_brain()
    {
        Type[] providerTypes = [typeof(object)];
        IServiceConfiguration[] serviceConfigurations = [Mock.Of<IServiceConfiguration>()];
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Brain"] = "127.0.0.1",
                ["ServerName"] = "test-server",
            })
            .Build();
        Mock<IBrainDiscovery> mockDiscovery = new(MockBehavior.Strict);
        Mock<ISdkServerStarter> mockStarter = new(MockBehavior.Strict);
        Mock<ISdkEnvironment> mockEnvironment = new(MockBehavior.Strict);
        mockEnvironment.Setup(environment => environment.HostAddress).Returns("http://127.0.0.1:1234");
        mockEnvironment.Setup(environment => environment.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockStarter
            .Setup(starter => starter.StartServerAsync(
                It.Is<Brain>(brain => brain.IPAddress.Equals(IPAddress.Parse("127.0.0.1"))),
                providerTypes,
                "test-server",
                serviceConfigurations,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(mockEnvironment.Object);
        Mock<IHostApplicationLifetime> mockLifetime = new(MockBehavior.Strict);
        SdkService service = new(providerTypes, serviceConfigurations, configuration, mockDiscovery.Object, mockStarter.Object, mockLifetime.Object, NullLogger<SdkService>.Instance);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await service.StartAsync(cancellationToken);
        await SdkServiceTests.WaitUntilAsync(() => mockStarter.Invocations.Count > 0, cancellationToken);
        // The HostAddress getter is read immediately after `this._environment` is assigned (the very next
        // statement in ExecuteAsync), so waiting for it proves the assignment has already happened before
        // we call StopAsync below.
        await SdkServiceTests.WaitUntilAsync(() => mockEnvironment.Invocations.Count > 0, cancellationToken);
        await service.StopAsync(cancellationToken);

        mockStarter.Verify(
            starter => starter.StartServerAsync(It.IsAny<Brain>(), providerTypes, "test-server", serviceConfigurations, It.IsAny<CancellationToken>()),
            Times.Once
        );
        mockEnvironment.Verify(environment => environment.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        // The configured Brain IP path must never invoke discovery - enforced implicitly by the strict mock
        // above having no setups (an unexpected call would have thrown inside ExecuteAsync).
    }

    [Fact]
    public async Task ExecuteAsync_starts_server_using_discovered_brain_when_none_configured()
    {
        Type[] providerTypes = [];
        IServiceConfiguration[] serviceConfigurations = [];
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ServerName"] = "test-server" })
            .Build();
        Brain discoveredBrain = new(IPAddress.Loopback);
        Mock<IBrainDiscovery> mockDiscovery = new(MockBehavior.Strict);
        mockDiscovery.Setup(discovery => discovery.DiscoverOneAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(discoveredBrain);
        Mock<ISdkServerStarter> mockStarter = new(MockBehavior.Strict);
        Mock<ISdkEnvironment> mockEnvironment = new(MockBehavior.Strict);
        mockEnvironment.Setup(environment => environment.HostAddress).Returns("http://127.0.0.1:1234");
        mockStarter
            .Setup(starter => starter.StartServerAsync(discoveredBrain, providerTypes, "test-server", serviceConfigurations, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEnvironment.Object);
        Mock<IHostApplicationLifetime> mockLifetime = new(MockBehavior.Strict);
        SdkService service = new(providerTypes, serviceConfigurations, configuration, mockDiscovery.Object, mockStarter.Object, mockLifetime.Object, NullLogger<SdkService>.Instance);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await service.StartAsync(cancellationToken);
        await SdkServiceTests.WaitUntilAsync(() => mockStarter.Invocations.Count > 0, cancellationToken);

        mockDiscovery.Verify(discovery => discovery.DiscoverOneAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        mockStarter.Verify(
            starter => starter.StartServerAsync(discoveredBrain, providerTypes, "test-server", serviceConfigurations, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_stops_application_when_discovery_is_cancelled()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        Mock<IBrainDiscovery> mockDiscovery = new(MockBehavior.Strict);
        mockDiscovery.Setup(discovery => discovery.DiscoverOneAsync(null, It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
        Mock<ISdkServerStarter> mockStarter = new(MockBehavior.Strict);
        Mock<IHostApplicationLifetime> mockLifetime = new(MockBehavior.Strict);
        mockLifetime.Setup(lifetime => lifetime.StopApplication());
        SdkService service = new([], [], configuration, mockDiscovery.Object, mockStarter.Object, mockLifetime.Object, NullLogger<SdkService>.Instance);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await service.StartAsync(cancellationToken);
        await SdkServiceTests.WaitUntilAsync(() => mockLifetime.Invocations.Count > 0, cancellationToken);

        mockLifetime.Verify(lifetime => lifetime.StopApplication(), Times.Once);
        // mockStarter has no setups - an unexpected call to StartServerAsync would have thrown above.
    }

    [Fact]
    public async Task ExecuteAsync_stops_application_when_discovery_fails()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        Mock<IBrainDiscovery> mockDiscovery = new(MockBehavior.Strict);
        mockDiscovery.Setup(discovery => discovery.DiscoverOneAsync(null, It.IsAny<CancellationToken>())).ThrowsAsync(new ApplicationException("Discovery failed, host stopping."));
        Mock<ISdkServerStarter> mockStarter = new(MockBehavior.Strict);
        Mock<IHostApplicationLifetime> mockLifetime = new(MockBehavior.Strict);
        mockLifetime.Setup(lifetime => lifetime.StopApplication());
        SdkService service = new([], [], configuration, mockDiscovery.Object, mockStarter.Object, mockLifetime.Object, NullLogger<SdkService>.Instance);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await service.StartAsync(cancellationToken);
        await SdkServiceTests.WaitUntilAsync(() => mockLifetime.Invocations.Count > 0, cancellationToken);

        mockLifetime.Verify(lifetime => lifetime.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_without_starting_does_not_touch_environment()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        Mock<IBrainDiscovery> mockDiscovery = new(MockBehavior.Strict);
        Mock<ISdkServerStarter> mockStarter = new(MockBehavior.Strict);
        Mock<IHostApplicationLifetime> mockLifetime = new(MockBehavior.Strict);
        SdkService service = new([], [], configuration, mockDiscovery.Object, mockStarter.Object, mockLifetime.Object, NullLogger<SdkService>.Instance);

        await service.StopAsync(TestContext.Current.CancellationToken);

        // No exception, and no ISdkEnvironment to touch since ExecuteAsync never ran.
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it's true or the timeout elapses. `BackgroundService`
    /// doesn't guarantee `ExecuteAsync`'s body runs synchronously within `StartAsync`, so tests need to
    /// wait for its background progress rather than asserting immediately.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        for (int i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
        Assert.True(condition(), "Timed out waiting for the expected call.");
    }
}
