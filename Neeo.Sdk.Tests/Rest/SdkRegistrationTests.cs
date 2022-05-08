using System;
using System.Collections.Generic;
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
    private readonly Lazy<object> _body;
    private readonly Lazy<string> _path;
    private readonly SdkRegistration _sdkRegistration;

    public SdkRegistrationTests()
    {
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(brain => brain.HostName).Returns(nameof(Brain));
        mockBrain.Setup(brain => brain.ServiceEndPoint).Returns(value: new(IPAddress.Loopback, 1234));
        Mock<IApiClient> mockApiClient = new(MockBehavior.Strict);
        List<object> list = new();
        mockApiClient
            .Setup(client => client.PostAsync(Capture.With<string>(new(list.Add)), Capture.With<object>(new(list.Add)), It.IsAny<Func<SuccessResponse, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        this._path = new(() => (string)list[0]);
        this._body = new(() => list[1]);
        Mock<ISdkEnvironment> mockSdkEnvironment = new(MockBehavior.Strict);
        mockSdkEnvironment.Setup(environment => environment.SdkAdapterName).Returns(Constants.SdkAdapterName);
        mockSdkEnvironment.Setup(environment => environment.HostAddress).Returns(Constants.HostAddress);
        this._sdkRegistration = new(mockBrain.Object, mockApiClient.Object, mockSdkEnvironment.Object, NullLogger<SdkRegistration>.Instance);
    }

    [Fact]
    public async Task StartAsync_should_register_using_correct_parameters()
    {
        await this._sdkRegistration.StartAsync(default).ConfigureAwait(false);
        Assert.Equal(UrlPaths.RegisterServer, this._path.Value);
        string bodyJson = JsonSerializer.Serialize(this._body.Value, JsonSerialization.Options);
        Assert.Equal($"{{\"name\":\"{Constants.SdkAdapterName}\",\"baseUrl\":\"{Constants.HostAddress}\"}}", bodyJson);
    }

    [Fact]
    public async Task StopAsync_should_unregister_using_correct_parameters()
    {
        await this._sdkRegistration.StopAsync(default).ConfigureAwait(false);
        Assert.Equal(UrlPaths.UnregisterServer, this._path.Value);
        string bodyJson = JsonSerializer.Serialize(this._body.Value, JsonSerialization.Options);
        Assert.Equal($"{{\"name\":\"{Constants.SdkAdapterName}\"}}", bodyJson);
    }

    private class Constants
    {
        public const string HostAddress = "http://192.168.1.1:123";
        public const string SdkAdapterName = nameof(SdkRegistrationTests);
    }
}