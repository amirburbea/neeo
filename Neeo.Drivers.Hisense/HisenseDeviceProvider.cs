using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseDeviceProvider : IDeviceProvider
{
    private static readonly Dictionary<Buttons, RemoteKey> _remoteKeys = new()
    {
        { Buttons.Back, RemoteKey.Back },
        { Buttons.CursorDown, RemoteKey.Down },
        { Buttons.CursorEnter, RemoteKey.OK },
        { Buttons.CursorLeft, RemoteKey.Left },
        { Buttons.CursorRight, RemoteKey.Right },
        { Buttons.CursorUp, RemoteKey.Up },
        { Buttons.Digit0, RemoteKey.Digit0 },
        { Buttons.Digit1, RemoteKey.Digit1 },
        { Buttons.Digit2, RemoteKey.Digit2 },
        { Buttons.Digit3, RemoteKey.Digit3 },
        { Buttons.Digit4, RemoteKey.Digit4 },
        { Buttons.Digit5, RemoteKey.Digit5 },
        { Buttons.Digit6, RemoteKey.Digit6 },
        { Buttons.Digit7, RemoteKey.Digit7 },
        { Buttons.Digit8, RemoteKey.Digit8 },
        { Buttons.Digit9, RemoteKey.Digit9 },
        { Buttons.Forward, RemoteKey.FastForward },
        { Buttons.Home, RemoteKey.Home },
        { Buttons.InputToggle, RemoteKey.InputToggle },
        { Buttons.Menu, RemoteKey.Menu },
        { Buttons.MuteToggle, RemoteKey.MuteToggle },
        { Buttons.Pause, RemoteKey.Pause },
        { Buttons.PowerOff, RemoteKey.Power },
        { Buttons.Play, RemoteKey.Play },
        { Buttons.Reverse, RemoteKey.Rewind },
        { Buttons.Stop, RemoteKey.Stop },
        { Buttons.VolumeDown, RemoteKey.VolumeDown },
        { Buttons.VolumeUp, RemoteKey.VolumeUp },
    };

    private readonly ILogger _logger;
    private HisenseTV[] _candidates = Array.Empty<HisenseTV>();
    private bool _connected;
    private IDeviceNotifier? _notifier;
    private HisenseTV? _tv;

    public HisenseDeviceProvider(ILogger<HisenseDeviceProvider> logger)
    {
        this._logger = logger;
        this.DeviceBuilder = Device.Create(Constants.DeviceName, DeviceType.TV)
            .SetManufacturer(Constants.Manufacturer)
            .SetSpecificName($"{Constants.Manufacturer} {Constants.DeviceName}")
            .AddButtonHandler(this.OnButtonPressed)
            .AddButtonGroup(ButtonGroups.Power | ButtonGroups.MenuAndBack | ButtonGroups.ControlPad | ButtonGroups.Volume | ButtonGroups.ChannelZapper | ButtonGroups.Transport | ButtonGroups.TransportSearch | ButtonGroups.NumberPad)
            .AddButton(Buttons.InputHdmi1 | Buttons.InputHdmi2 | Buttons.InputHdmi3 | Buttons.InputHdmi4 | Buttons.InputToggle)
            .AddSmartApplicationButton(SmartApplicationButtons.Amazon | SmartApplicationButtons.GooglePlay | SmartApplicationButtons.Netflix | SmartApplicationButtons.YouTube)
            .AddButton(Buttons.Home)
            .AddButton("HBOMAX", "HBO Max")
            .AddDirectory("Apps", default, DirectoryRole.Root, this.BrowseApps, this.LaunchApp)
            .AddPowerStateSensor(this.GetPowerState)
            .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
            .EnableNotifications(notifier => this._notifier = notifier)
            .AddSlider("VOLUME", null, this.GetVolumeAsync, this.SetVolumeAsync)
            .AddTextLabel("VOLUME-LABEL", "Volume", async (_) => (await this.GetVolumeAsync(_).ConfigureAwait(false)).ToString())
            .AddTextLabel("STATE", "State", this.GetStateAsync)
            .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync)
            .EnableRegistration("Registering TV...", "Enter the code showing on your TV. If no code is displayed, hit back and try again (after verifying the TV is on).", this.QueryIsRegistered, this.Register);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private async Task BrowseApps(string deviceId, IListBuilder list)
    {
        if (this._tv is not { } tv)
        {
            return;
        }
        list.AddTileRow(new ListTile("https://logodownload.org/wp-content/uploads/2019/11/hisense-logo.png"));
        AppInfo[] apps = Array.FindAll(await tv.GetAppsAsync().ConfigureAwait(false), static app => !app.IsUninstalled);
        (_, int limit, int? offset) = list.Parameters;
        if (offset is > 0 && limit < apps.Length)
        {
            int start = offset ?? 0;
            apps = apps[start..(start + limit)];
        }
        foreach ((string name, string url, _) in apps)
        {
            list.AddEntry(new(name, url, actionIdentifier: name));
        }
    }

    private Task<bool> GetPowerState(string deviceId) => Task.FromResult(this._tv != null && this._connected);

    private async Task<string> GetStateAsync(string deviceId)
    {
        return this._tv is { } tv && await tv.GetStateAsync().ConfigureAwait(false) is { } state
            ? state.ToString()!
            : string.Empty;
    }

    private async Task<double> GetVolumeAsync(string deviceId) => this._tv is { } tv
        ? await tv.GetVolumeAsync().ConfigureAwait(false)
        : 0d;

    private async Task InitializeDeviceList(string[] deviceIds)
    {
        this._logger.LogInformation("{method}:[{devices}]", nameof(this.InitializeDeviceList), string.Join(',', deviceIds));
        if (deviceIds.Length != 1)
        {
            return;
        }
        string macAddress = deviceIds[0];
        if (await HisenseTV.TryCreate(PhysicalAddress.Parse(macAddress), this._logger, connectionRequired: false).ConfigureAwait(false) is not { } tv)
        {
            this._logger.LogError("Failed to recreate TV based on mac address {macAddress}.", macAddress);
            return;
        }
        this.SetTV(tv, tv.IsConnected ? await tv.GetStateAsync().ConfigureAwait(false) : default);
    }

    private Task LaunchApp(string deviceId, string actionIdentifier) => this._tv?.LaunchAppAsync(actionIdentifier) ?? Task.CompletedTask;

    private Task OnButtonPressed(string deviceId, string buttonName)
    {
        if (this._tv is not { } tv)
        {
            return Task.CompletedTask;
        }
        if (Button.TryResolve(buttonName) is { } button)
        {
            return button switch
            {
                Buttons.PowerOn => tv.MacAddress.WakeAsync(),
                Buttons.InputHdmi1 => tv.ChangeSourceAsync("HDMI 1"),
                Buttons.InputHdmi2 => tv.ChangeSourceAsync("HDMI 2"),
                Buttons.InputHdmi3 => tv.ChangeSourceAsync("HDMI 3"),
                Buttons.InputHdmi4 => tv.ChangeSourceAsync("HDMI 4"),
                _ when HisenseDeviceProvider._remoteKeys.TryGetValue(button, out RemoteKey key) => tv.SendKeyAsync(key),
                _ => Task.CompletedTask
            };
        }
        if (SmartApplicationButton.TryResolve(buttonName) is { } smartButton)
        {
            return smartButton switch
            {
                SmartApplicationButtons.Netflix => tv.LaunchAppAsync("Netflix"),
                SmartApplicationButtons.YouTube => tv.LaunchAppAsync("YouTube"),
                SmartApplicationButtons.GooglePlay => tv.LaunchAppAsync("Play Store"),
                SmartApplicationButtons.Amazon => tv.LaunchAppAsync("Prime Video"),
                _ => Task.CompletedTask
            };
        }
        return buttonName switch
        {
            "HBOMAX" => tv.LaunchAppAsync("HBO Max"),
            _ => Task.CompletedTask
        };
    }

    private Task OnDeviceAdded(string deviceId) => Task.CompletedTask;

    private Task OnDeviceRemoved(string deviceId)
    {
        if (this._tv == null)
        {
            return Task.CompletedTask;
        }
        using HisenseTV tv = Interlocked.Exchange(ref this._tv, null);
        tv.VolumeChanged -= this.TV_VolumeChanged;
        tv.Sleep -= this.TV_Disconnected;
        tv.Disconnected -= this.TV_Disconnected;
        tv.Connected -= this.TV_Connected;
        tv.StateChanged -= this.TV_StateChanged;
        this._connected = false;
        return Task.CompletedTask;
    }

    private async Task<DiscoveredDevice[]> PerformDiscoveryAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        if (this._tv == null && optionalDeviceId != null &&
            await HisenseTV.TryCreate(PhysicalAddress.Parse(optionalDeviceId), this._logger, connectionRequired: true, cancellationToken: cancellationToken).ConfigureAwait(false) is { } tv &&
            await tv.GetStateAsync(cancellationToken).ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired } state)
        {
            this.SetTV(tv, state);
        }
        return (tv = this._tv) is not null
            ? new[] { new DiscoveredDevice(tv.MacAddress.ToString(), $"Hisense TV ({tv.MacAddress})", true) }
            : Array.Empty<DiscoveredDevice>();
    }

    private async Task<bool> QueryIsRegistered()
    {
        if (this._tv is { } tv)
        {
            return await tv.GetStateAsync().ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired };
        }
        HisenseTV[] tvs = await HisenseTV.DiscoverAsync(this._logger).ConfigureAwait(false);
        if (tvs.Length == 1 && await (tv = tvs[0]).GetStateAsync().ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired } state)
        {
            this.SetTV(tv, state);
            return true;
        }
        this._candidates = tvs;
        return false;
    }

    private async Task<RegistrationResult> Register(string code)
    {
        HisenseTV[] tvs = this._tv is { } tv ? new[] { tv } : this._candidates;
        for (int index = 0; index < tvs.Length; index++)
        {
            HisenseTV candidate = tvs[index];
            if (await candidate.AuthenticateAsync(code).ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired } state)
            {
                this.SetTV(candidate, state);
                // Dispose of the other candidates.
                for (int other = 0; other < tvs.Length; other++)
                {
                    if (other != index)
                    {
                        tvs[other].Dispose();
                    }
                }
                return RegistrationResult.Success;
            }
        }
        return RegistrationResult.Failed("Passcode was either incorrect or you took too long.");
    }

    private async void SetTV(HisenseTV tv, IState? state = default)
    {
        this._logger.LogInformation("Setting TV to {ipAddress}.", tv.IPAddress);
        this._tv = tv;
        this._connected = tv.IsConnected;
        tv.VolumeChanged += this.TV_VolumeChanged;
        tv.Sleep += this.TV_Disconnected;
        tv.Disconnected += this.TV_Disconnected;
        tv.Connected += this.TV_Connected;
        tv.StateChanged += this.TV_StateChanged;
        if (this._notifier is not { } notifier)
        {
            return;
        }
        string deviceId = tv.MacAddress.ToString();
        List<Task> list = new();
        if (state != null)
        {
            list.Add(notifier.SendNotificationAsync(deviceId, state.ToString()!, deviceId));
        }
        await Task.WhenAll(list).ConfigureAwait(false);
    }

    private async Task SetVolumeAsync(string deviceId, double value)
    {
        if (this._tv is { } tv)
        {
            await tv.ChangeVolumeAsync((int)value).ConfigureAwait(false);
        }
    }

    private async void TV_Connected(object? sender, EventArgs e)
    {
        if (this._tv is { } tv && this._notifier is { } notifier && !this._connected)
        {
            this._connected = true;
            await notifier.SendPowerNotificationAsync(true, tv.MacAddress.ToString()).ConfigureAwait(false);
        }
    }

    private async void TV_Disconnected(object? sender, EventArgs e)
    {
        if (this._tv is { } tv && this._notifier is { } notifier && this._connected)
        {
            this._connected = false;
            await notifier.SendPowerNotificationAsync(false, tv.MacAddress.ToString()).ConfigureAwait(false);
        }
    }

    private async void TV_StateChanged(object? sender, DataEventArgs<IState> e)
    {
        if (this._notifier is { } notifier && this._tv is { } tv)
        {
            await notifier.SendNotificationAsync("STATE", e.Data.ToString()!, tv.MacAddress.ToString()).ConfigureAwait(false);
        }
    }

    private async void TV_VolumeChanged(object? sender, DataEventArgs<int> e)
    {
        if (this._notifier is not { } notifier || this._tv is not { } tv)
        {
            return;
        }
        string deviceId = tv.MacAddress.ToString();
        await Task.WhenAll(
            notifier.SendNotificationAsync("VOLUME", (double)e.Data, deviceId),
            notifier.SendNotificationAsync("VOLUME-LABEL", e.Data.ToString(), deviceId)
        );
    }

    private readonly record struct DeviceTuple(IPAddress IPAddress, PhysicalAddress MacAddress);

    private static class Constants
    {
        public const string DeviceName = "IP Controlled TV";
        public const string Manufacturer = nameof(Hisense);
    }
}