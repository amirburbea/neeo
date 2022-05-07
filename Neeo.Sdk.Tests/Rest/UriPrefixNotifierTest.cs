using System;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Rest;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

public sealed class UriPrefixNotifierTest
{
    private readonly Mock<IDeviceDatabase> _mockDatabase = new();
    private readonly UriPrefixNotifier _uriPrefixNotifier;

    public UriPrefixNotifierTest()
    {
        Mock<ISdkEnvironment> mockEnvironment = new();
        mockEnvironment.SetupGet(environment => environment.HostAddress).Returns(Constants.HostAddress);
        this._uriPrefixNotifier = new(this._mockDatabase.Object, mockEnvironment.Object);
    }

    [Fact]
    public async Task Should_Notify_Correct_Uri_Prefix()
    {
        Mock<IDeviceAdapter> mockAdapter = new();
        this._mockDatabase.SetupGet(database => database.Adapters).Returns(new[] { mockAdapter.Object });
        mockAdapter.SetupGet(adapter => adapter.AdapterName).Returns(nameof(mockAdapter));
        string? uriPrefix = default;
        mockAdapter.SetupGet(adapter => adapter.UriPrefixCallback).Returns(SetUriPrefix);
        await this._uriPrefixNotifier.StartAsync(default).ConfigureAwait(false);
        Assert.Equal($"{Constants.HostAddress}/device/{nameof(mockAdapter)}/custom/", uriPrefix);

        ValueTask SetUriPrefix(string prefix)
        {
            uriPrefix = prefix;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_Notify_Multiple_Adapters_In_Parallel()
    {
        Mock<IDeviceAdapter> mockAdapter1 = new();
        Mock<IDeviceAdapter> mockAdapter2 = new();
        this._mockDatabase.SetupGet(database => database.Adapters).Returns(new[] { mockAdapter1.Object, mockAdapter2.Object });
        int[] threadIds = new int[2];
        SetUpMock(mockAdapter1, 0);
        SetUpMock(mockAdapter2, 1);
        await this._uriPrefixNotifier.StartAsync(default).ConfigureAwait(false);
        Assert.NotEqual(0, threadIds[0]);
        Assert.NotEqual(0, threadIds[1]);
        Assert.NotEqual(threadIds[0], threadIds[1]);

        void SetUpMock(Mock<IDeviceAdapter> mock, int index)
        {
            mock.SetupGet(adapter => adapter.AdapterName).Returns($"adapter{index}");
            mock.SetupGet(adapter => adapter.UriPrefixCallback).Returns(_ => SetUriPrefix(index));
        }

        async ValueTask SetUriPrefix(int index)
        {
            threadIds[index] = Environment.CurrentManagedThreadId;
            await Task.Delay(1).ConfigureAwait(false);
        }
    }

    private static class Constants
    {
        public const string HostAddress = "http://localhost:1234";
    }
}