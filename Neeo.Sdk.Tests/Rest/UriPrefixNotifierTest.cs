using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Rest;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

public sealed class UriPrefixNotifierTest
{
    private readonly Mock<IDeviceDatabase> _mockDatabase = new(MockBehavior.Strict);
    private readonly UriPrefixNotifier _uriPrefixNotifier;

    public UriPrefixNotifierTest()
    {
        Mock<ISdkEnvironment> mockEnvironment = new(MockBehavior.Strict);
        mockEnvironment.Setup(environment => environment.HostAddress).Returns(Constants.HostAddress);
        this._uriPrefixNotifier = new(this._mockDatabase.Object, mockEnvironment.Object);
    }

    [Fact]
    public async Task StartAsync_should_notify_correct_uri_prefix()
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockAdapter));
        string? uriPrefix = default;
        mockAdapter.Setup(adapter => adapter.UriPrefixCallback).Returns(prefix => uriPrefix = prefix);
        this._mockDatabase.Setup(database => database.Adapters).Returns([mockAdapter.Object]);

        await this._uriPrefixNotifier.StartAsync(default);

        Assert.Equal($"{Constants.HostAddress}/device/{nameof(mockAdapter)}/custom/", uriPrefix);
    }

    [Fact]
    public async Task StartAsync_should_notify_multiple_adapters_in_parallel()
    {
        const int count = 4;
        IDeviceAdapter[] adapters = new IDeviceAdapter[count];
        int[] threadIds = new int[count];
        for (int index = 0; index < count; index++)
        {
            Mock<IDeviceAdapter> mockAdapter = new();
            mockAdapter.Setup(adapter => adapter.AdapterName).Returns($"adapter{index}");
            int arrayIndex = index; // Capturing `index` causes issues.
            mockAdapter.Setup(adapter => adapter.UriPrefixCallback).Returns(_ => SetUriPrefix(arrayIndex));
            adapters[index] = mockAdapter.Object;
        }
        this._mockDatabase.Setup(database => database.Adapters).Returns(adapters);

        await this._uriPrefixNotifier.StartAsync(default);

        Assert.Equal(count, threadIds.Distinct().Count());
        Assert.DoesNotContain(0, threadIds);

        void SetUriPrefix(int index)
        {
            Thread.Sleep(100);
            threadIds[index] = Environment.CurrentManagedThreadId;
        }
    }

    private static class Constants
    {
        public const string HostAddress = "http://host";
    }
}
