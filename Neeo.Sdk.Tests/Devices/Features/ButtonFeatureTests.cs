using System;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;

public sealed class ButtonFeatureTests
{
    [Theory]
    [InlineData("POWER_ON")]
    [InlineData("POWER_OFF")]
    public async Task ExecuteAsync_should_pass_constructor_argument_to_handler(string buttonName)
    {
        Mock<ButtonHandler> mockButtonHandler = new(MockBehavior.Strict);
        mockButtonHandler.Setup(handler => handler(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        ButtonFeature feature = new(mockButtonHandler.Object, buttonName);
        string deviceId = Guid.NewGuid().ToString();
        await feature.ExecuteAsync(deviceId);

        mockButtonHandler.Verify(handler => handler(deviceId, buttonName), Times.Once());
    }
}
