using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class SubscriptionsNotifierTests
{
    private readonly Mock<IApiClient> _mockClient = new(MockBehavior.Strict);
    private readonly Mock<IDeviceDatabase> _mockDatabase = new(MockBehavior.Strict);
    private readonly SubscriptionsNotifier _notifier;

    public SubscriptionsNotifierTests()
    {
        Mock<ISdkEnvironment> mockEnvironment = new(MockBehavior.Strict);
        mockEnvironment.Setup(environment => environment.SdkAdapterName).Returns(Constants.SdkAdapterName);
        this._notifier = new(this._mockClient.Object, this._mockDatabase.Object, mockEnvironment.Object, NullLogger<SubscriptionsNotifier>.Instance);
    }

    [Fact]
    public async Task StartAsync_should_make_api_request_and_pass_result_to_adapter()
    {
        var adapter = this.CreateAdapter("adapter", true);
        this.SetAdapters(adapter);

        await this._notifier.StartAsync(default);

        Assert.Equal(Constants.GetAsyncCalled, adapter.SpecificName);
    }

    [Fact]
    public async Task StartAsync_should_only_make_api_request_on_adapters_with_subscription_support()
    {
        var adapterWith = this.CreateAdapter("adapter1", true);
        var adapterWithout = this.CreateAdapter("adapter2", false);
        this.SetAdapters(adapterWith, adapterWithout);

        await this._notifier.StartAsync(default);

        Assert.Equal(Constants.GetAsyncCalled, adapterWith.SpecificName);
        Assert.Equal(Constants.GetAsyncNotCalled, adapterWithout.SpecificName);
    }

    private IDeviceAdapter CreateAdapter(string adapterName, bool withSubscriptionFeature = false)
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.Manufacturer).Returns("NEEO");
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.SpecificName).Returns(Constants.GetAsyncNotCalled);
        mockAdapter.Setup(adapter => adapter.GetFeature(ComponentType.Subscription)).Returns(GetSubscriptionFeature());
        return mockAdapter.Object;

        ISubscriptionFeature? GetSubscriptionFeature()
        {
            if (!withSubscriptionFeature)
            {
                return default;
            }
            string path = string.Format(UrlPaths.SubscriptionsFormat, Constants.SdkAdapterName, adapterName);
            string[] ids = Array.ConvertAll(RandomNumberGenerator.GetBytes(5), static b => b.ToString());
            Mock<ISubscriptionFeature> mockFeature = new(MockBehavior.Strict);
            mockFeature
                .Setup(feature => feature.DeviceListInitializer)
                .Returns(DeviceSubscriptionHandler);
            this._mockClient
                .Setup(client => client.GetAsync(path, It.IsAny<Func<string[], Task>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, Func<string[], Task> transform, CancellationToken _) => transform(ids));
            return mockFeature.Object;

            Task DeviceSubscriptionHandler(string[] deviceIds)
            {
                Assert.Same(deviceIds, ids);
                this._mockClient.Verify(client => client.GetAsync(path, It.IsAny<Func<string[], Task>>(), It.IsAny<CancellationToken>()), Times.Once());
                mockAdapter.Setup(adapter => adapter.SpecificName).Returns(Constants.GetAsyncCalled);
                return Task.CompletedTask;
            }
        }
    }

    private void SetAdapters(params IDeviceAdapter[] adapters) => this._mockDatabase.Setup(database => database.Adapters).Returns(adapters);

    private static class Constants
    {
        public const string GetAsyncCalled = "GetAsync_called";
        public const string GetAsyncNotCalled = "GetAsync_not_called";
        public const string SdkAdapterName = "sdkAdapter";
    }
}