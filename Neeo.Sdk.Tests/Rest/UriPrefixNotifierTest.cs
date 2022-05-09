using System;
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
        this._mockDatabase.Setup(database => database.Adapters).Returns(new[] { mockAdapter.Object });
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(nameof(mockAdapter));
        string? uriPrefix = default;
        mockAdapter.Setup(adapter => adapter.UriPrefixCallback).Returns(SetUriPrefix);

        await this._uriPrefixNotifier.StartAsync(default);

        Assert.Equal($"{Constants.HostAddress}/device/{nameof(mockAdapter)}/custom/", uriPrefix);

        ValueTask SetUriPrefix(string prefix)
        {
            uriPrefix = prefix;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task StartAsync_should_notify_multiple_adapters_in_parallel()
    {
        Mock<IDeviceAdapter> mockAdapter1 = new(MockBehavior.Strict);
        Mock<IDeviceAdapter> mockAdapter2 = new(MockBehavior.Strict);
        this._mockDatabase.Setup(database => database.Adapters).Returns(new[] { mockAdapter1.Object, mockAdapter2.Object });
        int[] threadIds = new int[2];
        SetUpMock(mockAdapter1, 0);
        SetUpMock(mockAdapter2, 1);

        await this._uriPrefixNotifier.StartAsync(default);

        Assert.NotEqual(0, threadIds[0]);
        Assert.NotEqual(0, threadIds[1]);
        Assert.NotEqual(threadIds[0], threadIds[1]);

        void SetUpMock(Mock<IDeviceAdapter> mock, int index)
        {
            mock.Setup(adapter => adapter.AdapterName).Returns($"adapter{index}");
            mock.Setup(adapter => adapter.UriPrefixCallback).Returns(_ => SetUriPrefix(index));
        }

        async ValueTask SetUriPrefix(int index)
        {
            threadIds[index] = Environment.CurrentManagedThreadId;
            await Task.Delay(1);
        }
    }

    private static class Constants
    {
        public const string HostAddress = "http://localhost:1234";
    }
}