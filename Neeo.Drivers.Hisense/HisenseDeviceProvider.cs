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
        { KnownButtons.PlayPauseToggle, RemoteKey.Play },
        { KnownButtons.Reverse, RemoteKey.Rewind },
        { KnownButtons.Stop, RemoteKey.Stop },
        { KnownButtons.VolumeDown, RemoteKey.VolumeDown },
        { KnownButtons.VolumeUp, RemoteKey.VolumeUp },
    };

    private readonly ILogger<HisenseDeviceProvider> _logger;
    private HisenseTV[] _candidates = Array.Empty<HisenseTV>();
    private IDeviceNotifier? _notifier;
    private HisenseTV? _tv;

    public HisenseDeviceProvider(ILogger<HisenseDeviceProvider> logger)
    {
        this._logger = logger;
        //this._settingsFilePath = Path.Combine(
        //    Environment.GetFolderPath(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Environment.SpecialFolder.LocalApplicationData : Environment.SpecialFolder.UserProfile),
        //    "hisense.json"
        //);
    }

    public IDeviceBuilder ProvideDevice() => Device.Create(Constants.DriverName, DeviceType.TV)
        .SetSpecificName(Constants.DriverName)
        .SetDriverVersion(3)
        .SetManufacturer(Constants.Manufacturer)
        .AddButtonHandler(this.OnButtonPressed)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.MenuAndBack | ButtonGroups.ControlPad | ButtonGroups.Volume | ButtonGroups.ChannelZapper | ButtonGroups.TransportSearch)
        .AddButton(KnownButtons.InputHdmi1 | KnownButtons.InputHdmi2 | KnownButtons.InputHdmi3 | KnownButtons.InputHdmi4 | KnownButtons.InputToggle | KnownButtons.PlayPauseToggle | KnownButtons.Stop)
        .AddButton(KnownButtons.Amazon | KnownButtons.Netflix | KnownButtons.YouTube)
        .AddPowerStateSensor(this.GetPowerState)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
        .EnableNotifications(notifier => this._notifier = notifier)
        .AddSlider("VOLUME", null, this.GetVolumeAsync, this.SetVolumeAsync)
        .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync)
        .EnableRegistration("Registering TV...", "Enter the code showing on your TV.", this.QueryIsRegistered, this.Register);

    private Task<bool> GetPowerState(string deviceId) => Task.FromResult(this._tv != null && this._tv.IsConnected);

    private async Task<double> GetVolumeAsync(string deviceId)
    {
        if (_tv is { } tv)
        {
            return (int)await tv.GetVolumeAsync().ConfigureAwait(false);
        }
        return 0d;
    }

    private async Task InitializeDeviceList(string[] deviceIds)
    {
        if (deviceIds.Length == 1 && await HisenseTV.TryCreate(PhysicalAddress.Parse(deviceIds[0]), false, clientIdPrefix: Constants.ClientIdPrefix).ConfigureAwait(false) is { } tv)
        {
            SetTV(tv);
        }
    }

    private Task OnButtonPressed(string deviceId, string buttonName)
    {
        if (this._tv is { } tv && KnownButton.TryGetKnownButton(buttonName) is { } button)
        {
            return button switch
            {
                KnownButtons.PowerOn => tv.MacAddress.WakeAsync(),
                KnownButtons.InputHdmi1 => tv.ChangeSourceAsync("HDMI 1"),
                KnownButtons.InputHdmi2 => tv.ChangeSourceAsync("HDMI 2"),
                KnownButtons.InputHdmi3 => tv.ChangeSourceAsync("HDMI 3"),
                KnownButtons.InputHdmi4 => tv.ChangeSourceAsync("HDMI 4"),
                KnownButtons.Amazon => tv.LaunchAppAsync("Prime Video"),
                KnownButtons.Netflix => tv.LaunchAppAsync("Netflix"),
                KnownButtons.YouTube => tv.LaunchAppAsync("YouTube"),
                _ when _remoteKeys.TryGetValue(button, out RemoteKey key) => tv.SendKeyAsync(key),
                _ => Task.CompletedTask
            };
        }
        return Task.CompletedTask;
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
        if (_tv == null && optionalDeviceId != null)
        {
            if (await HisenseTV.TryCreate(PhysicalAddress.Parse(optionalDeviceId), true, clientIdPrefix: Constants.ClientIdPrefix, cancellationToken).ConfigureAwait(false) is { } tv)
            {
                if (await tv.GetStateAsync(cancellationToken).ConfigureAwait(false) is { Type: not StateType.AuthenticationRequired })
                {
                    SetTV(tv);
                }
            }
        }
        if (_tv == null)
        {
            throw new();
        }
        return new[] { new DiscoveredDevice(this._tv.MacAddress.ToString(), $"Hisense TV ({this._tv.IPAddress})", true) };
    }

    private async Task<bool> QueryIsRegistered()
    {
        if (this._tv != null)
        {
            return await _tv.GetStateAsync() is { Type: not StateType.AuthenticationRequired };
        }
        this._candidates = await HisenseTV.DiscoverAsync(clientIdPrefix: Constants.ClientIdPrefix);
        if (this._candidates is { Length: 1 } tvs && await tvs[0].GetStateAsync() is { Type: not StateType.AuthenticationRequired })
        {
            SetTV(tvs[0]);
            return true;
        }
        return false;
    }

    private async Task<RegistrationResult> Register(string code)
    {
        HisenseTV[] candidates;
        if (this._tv == null)
        {
            candidates = this._candidates;
        }
        else
        {
            candidates = new[] { this._tv };
        }
        for (int index = 0; index < candidates.Length; index++)
        {
            HisenseTV candidate = candidates[index];
            var state = await candidate.AuthenticateAsync(code);
            if (state.Type != StateType.AuthenticationRequired)
            {
                SetTV(candidate);
                for (int j = 0; j < candidates.Length; j++)
                {
                    if (j != index)
                    {
                        candidates[j].Dispose();
                    }
                }
                return RegistrationResult.Success;
            }
        }
        return RegistrationResult.Failed("You fucked up");

        //if ((state,_tv) is not ({ },{ }))
        //{
        //    return RegistrationResult.Failed("ERROR");
        //}
        //if (state.Type == StateType.AuthenticationRequired)
        //{
        //    state = await _tv.AuthenticateAsync(code);
        //}
        //if (state.Type == StateType.AuthenticationRequired)
        //{
        //    return RegistrationResult.Failed("You fucked up");
        //}
        //    return RegistrationResult.Success;
    }

    //private async Task InitializeAsync()
    //{
    //    if (!File.Exists(this._settingsFilePath))
    //    {
    //        return;
    //    }
    //    try
    //    {
    //        using Stream stream = File.OpenRead(this._settingsFilePath);
    //        if (await JsonSerializer.DeserializeAsync<string[]>(stream, JsonSerialization.Options).ConfigureAwait(false) is not { Length: > 1 } array)
    //        {
    //            return;
    //        }
    //        if (await HisenseTV.TryCreate(PhysicalAddress.Parse(array[0]), clientIdPrefix: nameof(HisenseDeviceProvider), connectionRequired: false) is { } tv)
    //        {
    //            this._tv = tv;
    //        }
    //    }
    //    catch (Exception)
    //    {
    //        // Ignore.
    //    }
    //}
    private void SetTV(HisenseTV tv)
    {
        this._tv = tv;
        tv.VolumeChanged += this.Tv_VolumeChanged;
        tv.Sleep += TV_Sleep;
    }

    private async Task SetVolumeAsync(string deviceId, double value)
    {
        if (_tv is { } tv)
        {
            await tv.ChangeVolumeAsync((int)value).ConfigureAwait(false);
        }
    }

    private async void TV_Sleep(object? sender, EventArgs e)
    {
        if (this._tv is { } tv && this._notifier is { } notifier)
        {
            await notifier.SendPowerNotificationAsync(false, tv.MacAddress.ToString()).ConfigureAwait(false);
        }
    }

    private async void Tv_VolumeChanged(object? sender, VolumeChangedEventArgs e)
    {
        if (this._notifier is { } notifier && this._tv is { } tv)
        {
            await notifier.SendNotificationAsync("VOLUME", e.Volume, tv.MacAddress.ToString()).ConfigureAwait(false);
        }
    }

    //private async Task<DeviceTuple?> RestoreDevice(IReadOnlyDictionary<IPAddress, PhysicalAddress> networkDevices)
    //{
    //    if (File.Exists(this._settingsFilePath))
    //    {
    //        try
    //        {
    //            using Stream stream = File.OpenRead(this._settingsFilePath);
    //            if (await JsonSerializer.DeserializeAsync<string[]>(stream, JsonSerialization.Options).ConfigureAwait(false) is { Length: > 0 } array)
    //            {
    //                PhysicalAddress macAddress = PhysicalAddress.Parse(array[0]);
    //                foreach ((IPAddress ipAddress, PhysicalAddress physicalAddress) in networkDevices)
    //                {
    //                    if (physicalAddress.Equals(macAddress))
    //                    {
    //                        return new(ipAddress, physicalAddress);
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception)
    //        {
    //            // Ignore.
    //        }
    //    }
    //    return null;
    //}

    private record struct DeviceTuple(IPAddress IPAddress, PhysicalAddress MacAddress);

    private static class Constants
    {
        public const string ClientIdPrefix = nameof(Neeo);
        public const string DriverName = "IP Controlled TV";
        public const string Manufacturer = nameof(Hisense);
    }
}