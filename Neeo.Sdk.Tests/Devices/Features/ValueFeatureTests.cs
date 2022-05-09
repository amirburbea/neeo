using System;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;

public sealed class ValueFeatureTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetValueAsync_should_use_BooleanBoxes_for_boolean_values(bool value)
    {
        Mock<DeviceValueGetter<bool>> mockGetter = new(MockBehavior.Strict);
        mockGetter.Setup(getter => getter(It.IsAny<string>())).ReturnsAsync(value);

        var feature = ValueFeature.Create(mockGetter.Object);
        var response = await feature.GetValueAsync(string.Empty);

        Assert.Same(BooleanBoxes.GetBox(value), response.Value);
    }

    [Theory]
    [InlineData(true, "True")]
    [InlineData(false, "false")]
    public async Task SetValueAsync_should_parse_boolean_values(bool value, string text)
    {
        Mock<DeviceValueSetter<bool>> mockSetter = new(MockBehavior.Strict);
        mockSetter.Setup(setter => setter(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        var feature = ValueFeature.Create(Mock.Of<DeviceValueGetter<bool>>(MockBehavior.Strict), mockSetter.Object);
        string deviceId = Guid.NewGuid().ToString();
        await feature.SetValueAsync(deviceId, text);

        mockSetter.Verify(setter => setter(deviceId, value), Times.Once());
    }

    [Theory]
    [InlineData(0d, "0")]
    [InlineData(.65, "0.65")]
    [InlineData(12345.0001, "00012345.00010000")]
    public async Task SetValueAsync_should_parse_double_values(double value, string text)
    {
        Mock<DeviceValueSetter<double>> mockSetter = new(MockBehavior.Strict);
        mockSetter.Setup(setter => setter(It.IsAny<string>(), It.IsAny<double>())).Returns(Task.CompletedTask);

        var feature = ValueFeature.Create(Mock.Of<DeviceValueGetter<double>>(MockBehavior.Strict), mockSetter.Object);
        string deviceId = Guid.NewGuid().ToString();
        await feature.SetValueAsync(deviceId, text);

        mockSetter.Verify(setter => setter(deviceId, value), Times.Once());
    }

    [Fact]
    public Task SetValueAsync_should_throw_when_created_without_setter()
    {
        var feature = ValueFeature.Create(Mock.Of<DeviceValueGetter<bool>>(MockBehavior.Strict));

        return Assert.ThrowsAsync<NotSupportedException>(() => feature.SetValueAsync(string.Empty, string.Empty));
    }
}