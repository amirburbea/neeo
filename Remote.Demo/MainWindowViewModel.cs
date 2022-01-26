using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using Neeo.Sdk;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Remote.Demo;

using static Neeo.Sdk.Devices.Device;

internal class MainWindowViewModel : NotifierBase
{
    private readonly Dispatcher _dispatcher;
    private IDeviceNotifier? _deviceNotifier;

    private bool _registered;

    public MainWindowViewModel(Brain brain)
    {
        this.Brain = brain;
        this._dispatcher = Dispatcher.CurrentDispatcher;
    }

    public Brain Brain { get; }

    public Device Device { get; } = new();

    public async Task StartServerAsync()
    {
        await this.Brain.StartServerAsync(new[] { this.CreateDeviceBuilder() }, "WPF");
        this.Device.PropertyChanged += this.Device_PropertyChanged;
    }

    public Task StopServerAsync()
    {
        this.Device.PropertyChanged -= this.Device_PropertyChanged;
        return this.Brain.StopServerAsync();
    }

    private IDeviceBuilder CreateDeviceBuilder() => CreateDevice("Example Device", DeviceType.TV)
        .AddAdditionalSearchTokens("WPF")
        .SetManufacturer("Amir")
        .SetIcon(DeviceIconOverride.Neeo)
        .SetDriverVersion(5)
        // Add button.
        .AddButton("INPUT HDMI1")
        .AddButton("INPUT HDMI2")
        // Add a known button via enumeration.
        .AddButtons(KnownButtons.Home)
        // Add multiple buttons at once via flagged enumeration.
        .AddButtons(KnownButtons.Netflix | KnownButtons.Amazon)
        // Add a button group via enumeration.
        .AddButtonGroups(ButtonGroups.Power)
        .AddButtonGroups(ButtonGroups.NumberPad)
        .AddButtonGroups(ButtonGroups.ControlPad | ButtonGroups.MenuAndBack | ButtonGroups.ChannelZapper | ButtonGroups.Volume)
        .AddButtonGroups(ButtonGroups.Transport | ButtonGroups.TransportScan | ButtonGroups.TransportSearch | ButtonGroups.TransportSkip)

        .SetSpecificName("Example Device")
        .AddButtonHandler(this.HandleButtonAsync)
        .AddSlider("Volume", "Volume", this.GetVolumeAsync, this.SetVolumeAsync)
        .AddSwitch("IsMuted", "IsMuted", this.GetIsMutedAsync, this.SetIsMutedAsync)
        .AddTextLabel("Volume-Label", "Volume", true, this.GetVolumeLabelAsync)
        .AddImageUrl("Small-Image", "small", ImageSize.Small, default, this.GetImageUriAsync)
        .AddImageUrl("Large-Image", "large", ImageSize.Large, default, this.GetImageUriAsync)
        .RegisterFavoritesHandler(this.HandleFavoritesAsync)
        .AddPowerStateSensor(this.GetPowerStateAsync)
        .EnableNotifications(this.RegisterDeviceNotifier)
        .RegisterDeviceSubscriptionCallbacks(this.HandleDeviceAddedAsync, this.HandleDeviceRemovedAsync, this.InitializeDeviceListAsync);

    private async void Device_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!this._registered || this._deviceNotifier is not { } notifier)
        {
            return;
        }
        switch (e.PropertyName)
        {
            case nameof(this.Device.IsPoweredOn):
                await notifier.SendPowerNotificationAsync(this.Device.IsPoweredOn);
                break;
            case nameof(this.Device.Volume):
                await Task.WhenAll(
                    notifier.SendNotificationAsync("Volume", this.Device.Volume),
                    notifier.SendNotificationAsync("Volume-Label", this.Device.Volume.ToString())
                );
                break;
            case nameof(this.Device.IsMuted):
                await Task.WhenAll(
                    notifier.SendNotificationAsync("IsMuted", BooleanBoxes.GetBox(this.Device.IsMuted)),
                    notifier.SendNotificationAsync("Volume-Label", this.Device.IsMuted ? "Muted" : this.Device.Volume.ToString())
                );
                break;
            case nameof(this.Device.ImageUri):
                await Task.WhenAll(
                    notifier.SendNotificationAsync("Small-Image", this.Device.ImageUri),
                    notifier.SendNotificationAsync("Large-Image", this.Device.ImageUri)
                );
                break;
        }
    }

    private Task<string> GetImageUriAsync(string deviceId)
    {
        return Task.FromResult(this.Device.ImageUri);
    }

    private Task<bool> GetIsMutedAsync(string deviceId) => Task.FromResult(this.Device.IsMuted);

    private Task<bool> GetPowerStateAsync(string deviceId) => Task.FromResult(this.Device.IsPoweredOn);

    private Task<double> GetVolumeAsync(string deviceId) => Task.FromResult(this.Device.Volume);

    private Task<string> GetVolumeLabelAsync(string deviceId) => Task.FromResult(this.Device.Volume.ToString());

    private Task HandleButtonAsync(string deviceId, string buttonName)
    {
        this.Device.ProcessButton(buttonName);
        return Task.CompletedTask;
    }

    private Task HandleDeviceAddedAsync(string deviceId)
    {
        this._registered = true;
        return Task.CompletedTask;
    }

    private Task HandleDeviceRemovedAsync(string deviceId)
    {
        this._registered = false;
        return Task.CompletedTask;
    }

    private Task HandleFavoritesAsync(string deviceId, string favorite)
    {
        this.Device.ProcessFavorite(favorite);
        return Task.CompletedTask;
    }

    private Task InitializeDeviceListAsync(string[] deviceIds)
    {
        this._registered = deviceIds.Length > 0;
        return Task.CompletedTask;
    }

    private void RegisterDeviceNotifier(IDeviceNotifier notifier) => this._deviceNotifier = notifier;

    private async Task SetIsMutedAsync(string deviceId, bool value) => await this._dispatcher.InvokeAsync(() => this.Device.IsMuted = value);

    private async Task SetVolumeAsync(string deviceId, double value) => await this._dispatcher.InvokeAsync(() => this.Device.Volume = value);
}