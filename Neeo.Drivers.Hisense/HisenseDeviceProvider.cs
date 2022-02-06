using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseDeviceProvider : IDeviceProvider
{
    private static readonly Dictionary<KnownButtons, RemoteKey> _remoteKeys = new()
    {
        { KnownButtons.Back, RemoteKey.Back },
        { KnownButtons.CursorDown, RemoteKey.Down },
        { KnownButtons.CursorEnter, RemoteKey.OK },
        { KnownButtons.CursorLeft, RemoteKey.Left },
        { KnownButtons.CursorRight, RemoteKey.Right },
        { KnownButtons.CursorUp, RemoteKey.Up },
        { KnownButtons.Digit0, RemoteKey.Digit0 },
        { KnownButtons.Digit1, RemoteKey.Digit1 },
        { KnownButtons.Digit2, RemoteKey.Digit2 },
        { KnownButtons.Digit3, RemoteKey.Digit3 },
        { KnownButtons.Digit4, RemoteKey.Digit4 },
        { KnownButtons.Digit5, RemoteKey.Digit5 },
        { KnownButtons.Digit6, RemoteKey.Digit6 },
        { KnownButtons.Digit7, RemoteKey.Digit7 },
        { KnownButtons.Digit8, RemoteKey.Digit8 },
        { KnownButtons.Digit9, RemoteKey.Digit9 },
        { KnownButtons.Forward, RemoteKey.FastForward },
        { KnownButtons.Home, RemoteKey.Home },
        { KnownButtons.InputToggle, RemoteKey.InputToggle },
        { KnownButtons.Menu, RemoteKey.Menu },
        { KnownButtons.MuteToggle, RemoteKey.MuteToggle },
        { KnownButtons.Pause, RemoteKey.Pause },
        { KnownButtons.PowerOff, RemoteKey.Power },
        { KnownButtons.Play, RemoteKey.Play },
        { KnownButtons.Reverse, RemoteKey.Rewind },
        { KnownButtons.Stop, RemoteKey.Stop },
        { KnownButtons.VolumeDown, RemoteKey.VolumeDown },
        { KnownButtons.VolumeUp, RemoteKey.VolumeUp },
    };

    private readonly ILogger<HisenseDeviceProvider> _logger;
    private HisenseTV[] _candidates = Array.Empty<HisenseTV>();
    private bool _connected = false;

    private IDeviceNotifier? _notifier;
    private HisenseTV? _tv;

    public HisenseDeviceProvider(ILogger<HisenseDeviceProvider> logger) => this._logger = logger;

    public IDeviceBuilder ProvideDevice() => Device.Create(Constants.DeviceName, DeviceType.TV)
        .SetDriverVersion(2)
        .SetManufacturer(Constants.Manufacturer)
        .SetSpecificName($"{Constants.Manufacturer} {Constants.DeviceName}")
        .AddButtonHandler(this.OnButtonPressed)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.MenuAndBack | ButtonGroups.ControlPad | ButtonGroups.Volume | ButtonGroups.ChannelZapper | ButtonGroups.Transport | ButtonGroups.TransportSearch | ButtonGroups.NumberPad)
        .AddButton(KnownButtons.InputHdmi1 | KnownButtons.InputHdmi2 | KnownButtons.InputHdmi3 | KnownButtons.InputHdmi4 | KnownButtons.InputToggle)
        .AddSmartAppButton(SmartAppButtons.Amazon | SmartAppButtons.GooglePlay | SmartAppButtons.Netflix | SmartAppButtons.YouTube)
        .AddButton(KnownButtons.Home)
        .AddButton("HBOMAX", "HBO Max")
        .AddPowerStateSensor(this.GetPowerState)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
        .EnableNotifications(notifier => this._notifier = notifier)
        .AddSlider("VOLUME", null, this.GetVolumeAsync, this.SetVolumeAsync)
        .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync)
        .EnableRegistration("Registering TV...", "Enter the code showing on your TV.", this.QueryIsRegistered, this.Register);

    private Task<bool> GetPowerState(string deviceId) => Task.FromResult(this._tv != null && this._tv.IsConnected);

    private async Task<double> GetVolumeAsync(string deviceId)
    {
        return this._tv is { } tv ? await tv.GetVolumeAsync().ConfigureAwait(false) : 0d;
    }

    private async Task InitializeDeviceList(string[] deviceIds)
    {
        if (deviceIds.Length == 1 && await HisenseTV.TryCreate(PhysicalAddress.Parse(deviceIds[0]), false, clientIdPrefix: Constants.ClientIdPrefix).ConfigureAwait(false) is { } tv)
        {
            this.SetTV(tv);
        }
    }

    private Task OnButtonPressed(string deviceId, string buttonName)
    {
        if (this._tv is not { } tv)
        {
            return Task.CompletedTask;
        }
        if (KnownButton.TryResolve(buttonName) is { } knownButton)
        {
            return knownButton switch
            {
                KnownButtons.PowerOn => tv.MacAddress.WakeAsync(),
                KnownButtons.InputHdmi1 => tv.ChangeSourceAsync("HDMI 1"),
                KnownButtons.InputHdmi2 => tv.ChangeSourceAsync("HDMI 2"),
                KnownButtons.InputHdmi3 => tv.ChangeSourceAsync("HDMI 3"),
                KnownButtons.InputHdmi4 => tv.ChangeSourceAsync("HDMI 4"),
                _ when _remoteKeys.TryGetValue(knownButton, out RemoteKey key) => tv.SendKeyAsync(key),
                _ => Task.CompletedTask
            };
        }
        if (SmartAppButton.TryResolve(buttonName) is { } smartAppButton)
        {
            return smartAppButton switch
            {
                SmartAppButtons.Netflix => tv.LaunchAppAsync("Netflix"),
                SmartAppButtons.YouTube => tv.LaunchAppAsync("YouTube"),
                SmartAppButtons.GooglePlay => tv.LaunchAppAsync("Play Store"),
                SmartAppButtons.Amazon => tv.LaunchAppAsync("Prime Video"),
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
        if (Interlocked.Exchange(ref this._tv, null) is { } tv)
        {
            tv.Dispose();
        }
        return Task.CompletedTask;
    }

    private async Task<DiscoveredDevice[]> PerformDiscoveryAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        if (this._tv == null && optionalDeviceId != null &&
            await HisenseTV.TryCreate(PhysicalAddress.Parse(optionalDeviceId), true, clientIdPrefix: Constants.ClientIdPrefix, cancellationToken).ConfigureAwait(false) is { } tv &&
            await tv.GetStateAsync(cancellationToken).ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired })
        {
            this.SetTV(tv);
        }
        return this._tv == null
            ? Array.Empty<DiscoveredDevice>()
            : new[] { new DiscoveredDevice(this._tv.MacAddress.ToString(), $"Hisense TV ({this._tv.IPAddress})", true) };
    }

    private async Task<bool> QueryIsRegistered()
    {
        if (this._tv != null)
        {
            return await _tv.GetStateAsync().ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired };
        }
        HisenseTV[] tvs = await HisenseTV.DiscoverAsync(clientIdPrefix: Constants.ClientIdPrefix).ConfigureAwait(false);
        if (tvs.Length == 1 && await tvs[0].GetStateAsync().ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired })
        {
            SetTV(tvs[0]);
            return true;
        }
        this._candidates = tvs;
        return false;
    }

    private async Task<RegistrationResult> Register(string code)
    {
        HisenseTV[] tvs = this._tv is { } tv ? new[] { tv } : this._candidates;
        try
        {
            foreach (HisenseTV candidate in tvs)
            {
                if ((await candidate.AuthenticateAsync(code).ConfigureAwait(false)).Type != StateType.AuthenticationRequired)
                {
                    SetTV(candidate);
                    return RegistrationResult.Success;
                }
            }
            return RegistrationResult.Failed("You fucked up");
        }
        finally
        {
            foreach (HisenseTV candidate in Interlocked.Exchange(ref this._candidates, Array.Empty<HisenseTV>()))
            {
                if (candidate != this._tv)
                {
                    candidate.Dispose();
                }
            }
        }
    }

    private void SetTV(HisenseTV tv)
    {
        this._logger.LogInformation("Setting TV to {ipAddress}.", tv.IPAddress);
        this._tv = tv;
        this._connected = tv.IsConnected;
        tv.VolumeChanged += this.TV_VolumeChanged;
        tv.Sleep += this.TV_Disconnected;
        tv.Disconnected += this.TV_Disconnected;
        tv.Connected += this.TV_Connected;
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

    private async void TV_VolumeChanged(object? sender, VolumeChangedEventArgs e)
    {
        if (this._notifier is { } notifier && this._tv is { } tv)
        {
            await notifier.SendNotificationAsync("VOLUME", (double)e.Volume, tv.MacAddress.ToString()).ConfigureAwait(false);
        }
    }

    private record struct DeviceTuple(IPAddress IPAddress, PhysicalAddress MacAddress);

    private static class Constants
    {
        public const string ClientIdPrefix = "NEEO";
        public const string DeviceName = "IP Controlled TV";
        public const string Manufacturer = nameof(Hisense);
    }
}