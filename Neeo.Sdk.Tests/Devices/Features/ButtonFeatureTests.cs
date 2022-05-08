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
        Mock<ButtonHandler> mockButtonHandler = new();
        ButtonFeature feature = new(mockButtonHandler.Object, buttonName);
        string deviceId = Guid.NewGuid().ToString();
        await feature.ExecuteAsync(deviceId).ConfigureAwait(false);
        mockButtonHandler.Verify(handler => handler(deviceId, buttonName), Times.Once());
    }
}