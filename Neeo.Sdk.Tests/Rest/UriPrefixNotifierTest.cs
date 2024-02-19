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
        mockEnvironment.Setup(environment => environment.HostAddress).Returns("http://localhost");
        this._uriPrefixNotifier = new(this._mockDatabase.Object, mockEnvironment.Object);
    }

    [Fact]
    public async Task StartAsync_should_notify_correct_uri_prefix()
    {
        Mock<IDeviceAdapter> mockDevice = new(MockBehavior.Strict);
        mockDevice.Setup(adapter => adapter.AdapterName).Returns(nameof(mockDevice));
        string? uriPrefix = default;
        mockDevice.Setup(adapter => adapter.UriPrefixCallback).Returns(prefix =>
        {
            uriPrefix = prefix;
            return ValueTask.CompletedTask;
        });
        this._mockDatabase.Setup(database => database.Adapters).Returns([mockDevice.Object]);

        await this._uriPrefixNotifier.StartAsync();

        Assert.Equal("http://localhost/device/mockDevice/custom/", uriPrefix);
    }
}