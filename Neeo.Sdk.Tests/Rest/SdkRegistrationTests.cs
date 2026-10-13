using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

public sealed class SdkRegistrationTests
{
    private readonly Lazy<string> _body;
    private readonly Lazy<string> _path;
    private readonly SdkRegistration _sdkRegistration;

    public SdkRegistrationTests()
    {
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(brain => brain.HostName).Returns(nameof(Brain));
        mockBrain.Setup(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        Mock<IApiClient> mockApiClient = new(MockBehavior.Strict);
        List<string> path = [];
        List<string> body = [];
        // Each method sends an anonymous type as the post body so we must set up with It.IsAnyType.
        // Capture is not compatible with It.IsAnyType so unlike the path we must capture the body within the returns method.
        mockApiClient
            .Setup(client => client.PostAsync(Capture.In(path), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(new InvocationAction(invocation => body.Add(JsonSerializer.Serialize(invocation.Arguments[1], JsonSerializerOptions.Web))));
        mockApiClient
            .Setup(client => client.IsAliveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        this._path = new(path.Single);
        this._body = new(body.Single);
        Mock<ISdkEnvironment> mockSdkEnvironment = new(MockBehavior.Strict);
        mockSdkEnvironment.Setup(environment => environment.SdkAdapterName).Returns(Constants.SdkAdapterName);
        mockSdkEnvironment.Setup(environment => environment.HostAddress).Returns(Constants.HostAddress);
        this._sdkRegistration = new(mockBrain.Object, mockApiClient.Object, mockSdkEnvironment.Object, NullLogger<SdkRegistration>.Instance);
    }

    [Fact]
    public async Task StartAsync_should_register_using_correct_parameters()
    {
        await this._sdkRegistration.StartAsync(TestContext.Current.CancellationToken);

        Assert.True(this._sdkRegistration.IsRegistered);
        Assert.Equal(BrainUrlPaths.RegisterServer, this._path.Value);
        Assert.Equal($"{{\"name\":\"{Constants.SdkAdapterName}\",\"baseUrl\":\"{Constants.HostAddress}\"}}", this._body.Value);
    }

    [Fact]
    public async Task StopAsync_should_unregister_using_correct_parameters()
    {
        await this._sdkRegistration.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(this._sdkRegistration.IsRegistered);
        Assert.Equal(BrainUrlPaths.UnregisterServer, this._path.Value);
        Assert.Equal($"{{\"name\":\"{Constants.SdkAdapterName}\"}}", this._body.Value);
    }

    [Fact]
    public async Task Heartbeat_should_re_register_only_after_dropped_connection()
    {
        Mock<IBrain> mockBrain = new();
        mockBrain.Setup(b => b.HostName).Returns("Brain");
        mockBrain.Setup(b => b.ServiceEndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 1234));

        Mock<ISdkEnvironment> mockEnvironment = new();
        mockEnvironment.Setup(e => e.SdkAdapterName).Returns("adapter");
        mockEnvironment.Setup(e => e.HostAddress).Returns("http://127.0.0.1:5000");

        int isAliveProbeCount = 0;
        int registerPostCount = 0;
        TaskCompletionSource<bool> probeThreeTcs = new();

        Mock<IApiClient> mockClient = new();
        mockClient
            .Setup(c => c.IsAliveAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                int count = Interlocked.Increment(ref isAliveProbeCount);
                if (count == 1)
                {
                    // Probe 1 fails (Brain is rebooting / offline)
                    return Task.FromResult(false);
                }
                if (count == 2)
                {
                    // Probe 2 succeeds (Brain finished rebooting, now reachable)
                    return Task.FromResult(true);
                }
                // Subsequent probes: Brain remains healthy
                probeThreeTcs.TrySetResult(true);
                return Task.FromResult(true);
            });

        mockClient
            .Setup(c => c.PostAsync(BrainUrlPaths.RegisterServer, It.IsAny<SdkRegistration.RegisterServerRequest>(), It.IsAny<CancellationToken>()))
            .Returns<string, SdkRegistration.RegisterServerRequest, CancellationToken>((_, _, _) =>
            {
                Interlocked.Increment(ref registerPostCount);
                return Task.FromResult(true);
            });

        mockClient
            .Setup(c => c.PostAsync(BrainUrlPaths.UnregisterServer, It.IsAny<SdkRegistration.UnregisterServerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SdkRegistration registration = new(
            mockBrain.Object,
            mockClient.Object,
            mockEnvironment.Object,
            NullLogger<SdkRegistration>.Instance,
            heartbeatInterval: TimeSpan.FromMilliseconds(20)
        );

        await registration.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(registration.IsRegistered);
        // Only 1 registration POST on initial startup
        Assert.Equal(1, registerPostCount);

        TaskCompletionSource<bool> droppedTcs = new();
        TaskCompletionSource<bool> restoredTcs = new();
        registration.RegistrationStateChanged += registered =>
        {
            if (!registered)
            {
                droppedTcs.TrySetResult(true);
            }
            else
            {
                restoredTcs.TrySetResult(true);
            }
        };

        // Wait for probe 1 (which reports false, marking connection dropped)
        await droppedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.False(registration.IsRegistered);
        // Brain was offline, so NO new register POST attempted
        Assert.Equal(1, registerPostCount);

        // Wait for probe 2 (which reports true, edge-triggering recovery registration)
        await restoredTcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(registration.IsRegistered);
        // Re-registration POST triggered exactly once on recovery
        Assert.Equal(2, registerPostCount);

        // Wait for probe 3 to confirm that subsequent healthy checks do NOT spam registration POSTs
        await probeThreeTcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(registration.IsRegistered);
        Assert.Equal(2, registerPostCount);

        await registration.StopAsync(TestContext.Current.CancellationToken);
        Assert.False(registration.IsRegistered);
    }

    private class Constants
    {
        public const string HostAddress = "http://192.168.1.1:123";
        public const string SdkAdapterName = nameof(SdkRegistrationTests);
    }
}
