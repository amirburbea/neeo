using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

public class SimpleTVExampleDevice : IDeviceProvider
{
    private readonly ILogger<SimpleTVExampleDevice> _logger;

    public SimpleTVExampleDevice(ILogger<SimpleTVExampleDevice> logger) => this._logger = logger;

    public IDeviceBuilder ProvideDevice()
    {
        const string deviceName = "Simple TV Example Device";
        return Device.Create(deviceName, DeviceType.TV)
            .SetSpecificName(deviceName)
            .SetManufacturer("NEEO")
            .SetIcon(DeviceIconOverride.NeeoBrain)
            .AddButton(KnownButtons.InputHdmi1) // Add a known button.
            .AddButton(KnownButtons.InputHdmi2 | KnownButtons.InputHdmi3) // Add multiple known buttons at once.
            .AddButton("SomeRandomName", "Randomly Named") // Add a button with a custom label.
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.ChannelZapper | ButtonGroups.Volume | ButtonGroups.MenuAndBack)
            .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
            .AddButtonHandler(this.OnButtonPressed);
    }

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