using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples;

public sealed class FavoritesExampleDeviceProvider : IDeviceProvider
{
    private readonly ILogger<FavoritesExampleDeviceProvider> _logger;
    private double _sliderValue;
    private bool _switchValue;

    public FavoritesExampleDeviceProvider(ILogger<FavoritesExampleDeviceProvider> logger)
    {
        const string deviceName = "SDK Favorites Example";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.TV)
            .SetSpecificName(deviceName)
            .AddButtonGroup( ButtonGroups.NumberPad | ButtonGroups.Volume)
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .AddButton("INPUT HDMI1")
            .AddButtonHandler(this.HandleButtonAsync)
            .AddFavoriteHandler(this.HandleFavoriteAsync)
            .AddSlider("SLIDER", "Slider", deviceId => this._sliderValue, (deviceId, value) => this._sliderValue = value, 0, 100)
            .AddSwitch("SWITCH", "Switch", deviceId => this._switchValue, (deviceId, value) => this._switchValue = value);
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
