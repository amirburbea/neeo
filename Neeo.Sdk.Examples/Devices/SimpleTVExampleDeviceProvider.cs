using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

public class SimpleTVExampleDeviceProvider : IDeviceProvider
{
    private readonly ILogger<SimpleTVExampleDeviceProvider> _logger;

    private IDeviceNotifier? _notifier;

    public SimpleTVExampleDeviceProvider(ILogger<SimpleTVExampleDeviceProvider> logger)
    {
        this._logger = logger;
        const string deviceName = "SDK Simple TV Example Device";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.TV)
            .SetDriverVersion(2)
            .SetSpecificName(deviceName)
            .AddButton(Buttons.InputHdmi1) // Add a known button.
            .AddButton(Buttons.InputHdmi2 | Buttons.InputHdmi3) // Add multiple known buttons at once.
            .AddButton("SomeRandomName", "Randomly Named") // Add a button with a custom label.
            .AddButton(Buttons.Home)
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.ChannelZapper | ButtonGroups.Volume | ButtonGroups.MenuAndBack)
            .AddTextLabel("my-label", "label", getter: (_, _) => Task.FromResult("no-label-value"), true)
            .EnableNotifications(this.SetNotifier)
            .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
            .AddButtonHandler(this.OnButtonPressed);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private Task InitializeDeviceList(string[] deviceIds, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Initialized with [{deviceIds}]", string.Join(',', deviceIds));
        return Task.CompletedTask;
    }

    private async Task OnButtonPressed(string deviceId, string button, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Button {button} pressed on device: {deviceId}.", button, deviceId);
        if (button == "SomeRandomName" && this._notifier is { } notifier)
        {
            await notifier.SendNotificationAsync("my-label", "abcde", deviceId, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task OnDeviceAdded(string deviceId, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Device added '{deviceId}'", deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceRemoved(string deviceId, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Device removed '{deviceId}'", deviceId);
        return Task.CompletedTask;
    }

    private void SetNotifier(IDeviceNotifier notifier)
    {
        this._notifier = notifier;
    }
}
