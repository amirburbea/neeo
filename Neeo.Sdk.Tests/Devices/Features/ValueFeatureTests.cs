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
        var response = await feature.GetValueAsync(string.Empty).ConfigureAwait(false);
        Assert.Same(BooleanBoxes.GetBox(value), response.Value);
    }

    [Theory]
    [InlineData(true, "True")]
    [InlineData(false, "false")]
    public async Task SetValueAsync_should_parse_boolean_values(bool value, string text)
    {
        string deviceId = Guid.NewGuid().ToString();
        Mock<DeviceValueSetter<bool>> mockSetter = new();
        mockSetter.Setup(setter => setter(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        var feature = ValueFeature.Create(Mock.Of<DeviceValueGetter<bool>>(), mockSetter.Object);
        await feature.SetValueAsync(deviceId, text).ConfigureAwait(false);
        mockSetter.Verify(setter => setter(deviceId, value), Times.Once());
    }

    [Theory]
    [InlineData(0d, "0")]
    [InlineData(.65, "0.65")]
    [InlineData(12345.0001, "00012345.00010000")]
    public async Task SetValueAsync_should_parse_double_values(double value, string text)
    {
        string deviceId = Guid.NewGuid().ToString();
        Mock<DeviceValueSetter<double>> mockSetter = new();
        mockSetter.Setup(setter => setter(It.IsAny<string>(), It.IsAny<double>())).Returns(Task.CompletedTask);
        var feature = ValueFeature.Create(Mock.Of<DeviceValueGetter<double>>(), mockSetter.Object);
        await feature.SetValueAsync(deviceId, text).ConfigureAwait(false);
        mockSetter.Verify(setter => setter(deviceId, value), Times.Once());
    }

    [Fact]
    public Task SetValueAsync_should_throw_when_no_setter()
    {
        var feature = ValueFeature.Create(Mock.Of<DeviceValueGetter<bool>>());
        return Assert.ThrowsAsync<NotSupportedException>(() => feature.SetValueAsync(string.Empty, string.Empty));
    }
}