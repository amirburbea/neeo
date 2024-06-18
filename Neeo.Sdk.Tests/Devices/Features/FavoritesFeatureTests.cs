using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;

public sealed class FavoritesFeatureTests
{
    [Theory]
    [InlineData("POWER_ON")]
    [InlineData("POWER_OFF")]
    public async Task ExecuteAsync_should_pass_correct_arguments_to_handler(string favorite)
    {
        Mock<FavoriteHandler> mockFavoriteHandler = new(MockBehavior.Strict);
        mockFavoriteHandler.Setup(handler => handler(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        FavoritesFeature feature = new(mockFavoriteHandler.Object);
        string deviceId = Guid.NewGuid().ToString();
        CancellationToken cancellationToken = new();
        await feature.ExecuteAsync(deviceId, favorite, cancellationToken);

        mockFavoriteHandler.Verify(handler => handler(deviceId, favorite, cancellationToken), Times.Once());
    }
}
