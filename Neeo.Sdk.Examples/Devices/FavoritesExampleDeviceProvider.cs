using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

public sealed class FavoritesExampleDeviceProvider : IDeviceProvider
{
    private readonly ILogger<FavoritesExampleDeviceProvider> _logger;

    public FavoritesExampleDeviceProvider(ILogger<FavoritesExampleDeviceProvider> logger)
    {
        const string deviceName = "Favorites Example";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.TV)
            .SetSpecificName(deviceName)
            .AddButtonGroup(ButtonGroups.Power | ButtonGroups.NumberPad | ButtonGroups.Volume)
            .AddButton("INPUT HDMI1")
            .AddButtonHandler(this.HandleButtonAsync)
            .AddFavoriteHandler(this.HandleFavoriteAsync);
        this._logger = logger;
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private Task HandleButtonAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("BUTTON: {buttonName}", buttonName);
        return Task.CompletedTask;
    }

    private Task HandleFavoriteAsync(string deviceId, string favorite, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("FAVORITE: {favorite}", favorite);
        return Task.CompletedTask;
    }
}
