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
    public async Task StartAsync_should()
    {
        var adapter = this.CreateAdapter("adapter", true);
        this.SetAdapters(adapter);
        await this._notifier.StartAsync(default).ConfigureAwait(false);
        Assert.Equal(Constants.GetAsyncCalled, adapter.SpecificName);
    }

    private IDeviceAdapter CreateAdapter(string adapterName, bool withSubscriptionFeature = false)
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.Manufacturer).Returns("NEEO");
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.SpecificName).Returns("GetAsync_not_called");
        mockAdapter
            .Setup(adapter => adapter.GetFeature(ComponentType.Subscription))
            .Returns(withSubscriptionFeature ? CreateSubscriptionFeature() : default(IFeature));
        return mockAdapter.Object;

        ISubscriptionFeature CreateSubscriptionFeature()
        {
            string path = string.Format(UrlPaths.SubscriptionsFormat, Constants.SdkAdapterName, adapterName);
            string[] ids = Array.ConvertAll(RandomNumberGenerator.GetBytes(5), static b => b.ToString());
            Mock<ISubscriptionFeature> mockFeature = new(MockBehavior.Strict);
            mockFeature
                .Setup(feature => feature.DeviceListInitializer)
                .Returns(DeviceSubscriptionHandler);
            this._mockClient
                .Setup(client => client.GetAsync(path, It.IsAny<Func<string[], Task>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string path, Func<string[], Task> transform, CancellationToken _) => transform(ids));
            return mockFeature.Object;

            Task DeviceSubscriptionHandler(string[] deviceIds)
            {
                Assert.StrictEqual(deviceIds, ids);
                this._mockClient.Verify(client => client.GetAsync(path, It.IsAny<Func<string[], Task>>(), It.IsAny<CancellationToken>()), Times.Once());
                mockAdapter.Setup(adapter => adapter.SpecificName).Returns(Constants.GetAsyncCalled);
                return Task.CompletedTask;
            }
        }
    }

    private void SetAdapters(params IDeviceAdapter[] adapters) => this._mockDatabase.Setup(database => database.Adapters).Returns(adapters);

    private static class Constants
    {
        public const string SdkAdapterName = "sdkAdapter";
        public const string GetAsyncCalled = "GetAsync_called";
    }
}