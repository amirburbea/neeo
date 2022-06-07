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
        List<string> path = new();
        List<string> body = new();
        // Each method sends an anonymous type as the post body so we must set up with It.IsAnyType.
        // Capture is not compatible with It.IsAnyType so unlike the path we must capture the body within the returns method.
        mockApiClient
            .Setup(client => client.PostAsync(Capture.In(path), It.IsAny<It.IsAnyType>(), It.IsAny<Func<SuccessResponse, It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .ReturnsTransformOf(new SuccessResponse(true))
            .Callback(new InvocationAction(invocation => body.Add(JsonSerializer.Serialize(invocation.Arguments[1], JsonSerialization.Options))));
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
        await this._sdkRegistration.StartAsync(default);

        Assert.Equal(UrlPaths.RegisterServer, this._path.Value);
        Assert.Equal($"{{\"name\":\"{Constants.SdkAdapterName}\",\"baseUrl\":\"{Constants.HostAddress}\"}}", this._body.Value);
    }

    [Fact]
    public async Task StopAsync_should_unregister_using_correct_parameters()
    {
        await this._sdkRegistration.StopAsync(default);

        Assert.Equal(UrlPaths.UnregisterServer, this._path.Value);
        Assert.Equal($"{{\"name\":\"{Constants.SdkAdapterName}\"}}", this._body.Value);
    }

    private class Constants
    {
        public const string HostAddress = "http://192.168.1.1:123";
        public const string SdkAdapterName = nameof(SdkRegistrationTests);
    }
}