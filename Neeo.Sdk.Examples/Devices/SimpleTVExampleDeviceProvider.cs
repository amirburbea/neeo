using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

public class SimpleTVExampleDeviceProvider : IDeviceProvider
{
    private readonly ILogger<SimpleTVExampleDeviceProvider> _logger;

    public SimpleTVExampleDeviceProvider(ILogger<SimpleTVExampleDeviceProvider> logger)
    {
        this._logger = logger;
        const string deviceName = "Simple TV Example Device";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.TV)
            .SetSpecificName(deviceName)
            .SetIcon(DeviceIconOverride.NeeoBrain)
            .AddButton(Buttons.InputHdmi1) // Add a known button.
            .AddButton(Buttons.InputHdmi2 | Buttons.InputHdmi3) // Add multiple known buttons at once.
            .AddButton("SomeRandomName", "Randomly Named") // Add a button with a custom label.
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.ChannelZapper | ButtonGroups.Volume | ButtonGroups.MenuAndBack)
            .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
            .AddButtonHandler(this.OnButtonPressed);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private Task InitializeDeviceList(string[] deviceIds)
    {
        this._logger.LogInformation("Initialized with [{deviceIds}]", string.Join(',', deviceIds));
        return Task.CompletedTask;
    }

    private Task OnButtonPressed(string deviceId, string button)
    {
        this._logger.LogInformation("Button {button} pressed on device: {deviceId}.", button, deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceAdded(string deviceId)
    {
        this._logger.LogInformation("Device added '{deviceId}'", deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceRemoved(string deviceId)
    {
        this._logger.LogInformation("Device removed '{deviceId}'", deviceId);
        return Task.CompletedTask;
    }
}