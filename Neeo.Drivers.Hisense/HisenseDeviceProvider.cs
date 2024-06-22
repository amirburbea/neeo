using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseDeviceProvider(ILogger<HisenseDeviceProvider> logger) : IDeviceProvider
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

    private readonly HashSet<string> _deviceIds = [];
    private HisenseTV[] _candidates = [];
    private bool _connected;
    private IDeviceBuilder? _deviceBuilder;
    private IDeviceNotifier? _notifier;
    private HisenseTV? _tv;

    public IDeviceBuilder DeviceBuilder => this._deviceBuilder ??= Device.Create(Constants.DeviceName, DeviceType.TV)
        .SetManufacturer(Constants.Manufacturer)
        .SetSpecificName($"{Constants.Manufacturer} {Constants.DeviceName}")
        .AddButtonHandler(this.OnButtonPressed)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.MenuAndBack | ButtonGroups.ControlPad | ButtonGroups.Volume | ButtonGroups.ChannelZapper | ButtonGroups.Transport | ButtonGroups.TransportSearch | ButtonGroups.NumberPad)
        .AddButton(Buttons.InputHdmi1 | Buttons.InputHdmi2 | Buttons.InputHdmi3 | Buttons.InputHdmi4 | Buttons.InputToggle)
        .AddSmartApplicationButton(SmartApplicationButtons.Amazon | SmartApplicationButtons.GooglePlay | SmartApplicationButtons.Netflix | SmartApplicationButtons.YouTube)
        .AddButton(Buttons.Home)
        .AddButton("HBOMAX", "HBO Max")
        .AddDirectory("Apps", default, DirectoryRole.Root, this.BrowseApps, this.LaunchApp)
        .AddPowerStateSensor((deviceId, _) => this.GetPowerState(deviceId))
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
        .EnableNotifications(notifier => this._notifier = notifier)
        .AddSlider("VOLUME", null, this.GetVolumeAsync, this.SetVolumeAsync)
        .AddTextLabel("VOLUME-LABEL", "Volume", async (deviceId, cancellationToken) => (await this.GetVolumeAsync(deviceId, cancellationToken).ConfigureAwait(false)).ToString())
        .AddTextLabel("STATE", "State", this.GetStateAsync)
        .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync)
        .EnableRegistration(
            "Registering TV...",
            "Enter the code showing on your TV. If no code is displayed, try 0000 or hit back and try again (after verifying the TV is on).",
            this.QueryIsRegistered,
            this.Register
        );

    private static Task<IState?> AuthenticatedStateAsync(HisenseTV tv, string? code = null, CancellationToken cancellationToken = default)
    {
        return code == null
            ? tv.GetStateAsync(cancellationToken).ContinueWith(task => AuthenticatedState(task.Result), TaskContinuationOptions.ExecuteSynchronously)
            : tv.AuthenticateAsync(code, cancellationToken).ContinueWith(task => AuthenticatedState(task.Result), TaskContinuationOptions.ExecuteSynchronously);

        static IState? AuthenticatedState(IState? state) => state is { Type: not StateType.AuthenticationRequired } ? state : default;
    }

    private static Task NotifyStateAsync(IDeviceNotifier notifier, HisenseTV tv, IState state) => notifier.SendNotificationAsync("STATE", state.ToString(), tv.DeviceId);

    private async Task BrowseApps(string deviceId, DirectoryBuilder list, CancellationToken cancellationToken)
    {
        if (this._tv is not { } tv)
        {
            return;
        }
        list.AddTileRow(new DirectoryTile("https://logodownload.org/wp-content/uploads/2019/11/hisense-logo.png"));
        AppInfo[] apps = Array.FindAll(await tv.GetAppsAsync(cancellationToken).ConfigureAwait(false), static app => !app.IsUninstalled);
        Array.Sort(apps, (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
        (_, int limit, int? offset) = list.Parameters;
        if (offset is > 0 && limit < apps.Length)
        {
            int start = offset ?? 0;
            apps = apps[start..(start + limit)];
        }
        foreach ((string name, string url, _) in apps)
        {
            list.AddEntry(new(name, url, ActionIdentifier: name));
        }
    }

    private Task<bool> GetPowerState(string deviceId) => Task.FromResult(this._tv != null && this._connected);

    private async Task<string> GetStateAsync(string deviceId, CancellationToken cancellationToken)
    {
        return this._tv is { } tv && await tv.GetStateAsync(cancellationToken).ConfigureAwait(false) is { } state
            ? state.ToString()
            : string.Empty;
    }

    private async Task<double> GetVolumeAsync(string deviceId, CancellationToken cancellationToken) => this._tv is { } tv
        ? await tv.GetVolumeAsync(cancellationToken).ConfigureAwait(false)
        : 0d;

    private async Task InitializeDeviceList(string[] deviceIds, CancellationToken cancellationToken)
    {
        logger.LogInformation("{method}:[{devices}]", nameof(this.InitializeDeviceList), string.Join(',', deviceIds));
        Array.ForEach(deviceIds, deviceId => this._deviceIds.Add(deviceId));
        if (deviceIds is not [{ } macAddress])
        {
            return;
        }
        if (await HisenseTV.TryCreateAsync(PhysicalAddress.Parse(macAddress), logger, useCertificates: true, cancellationToken: cancellationToken).ConfigureAwait(false) is not { } tv)
        {
            logger.LogError("Failed to recreate TV based on mac address {macAddress}.", macAddress);
            return;
        }
        this.SetTV(tv, tv.IsConnected ? await tv.GetStateAsync(cancellationToken).ConfigureAwait(false) : default);
    }

    private Task LaunchApp(string deviceId, string actionIdentifier, CancellationToken cancellationToken) => this._tv?.LaunchAppAsync(actionIdentifier, cancellationToken) ?? Task.CompletedTask;

    private Task OnButtonPressed(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        if (this._tv is not { } tv)
        {
            return Task.CompletedTask;
        }
        if (Button.TryResolve(buttonName) is { } button)
        {
            return button switch
            {
                Buttons.PowerOn => tv.MacAddress.WakeAsync(cancellationToken),
                Buttons.InputHdmi1 => tv.ChangeSourceAsync("HDMI 1", cancellationToken),
                Buttons.InputHdmi2 => tv.ChangeSourceAsync("HDMI 2", cancellationToken),
                Buttons.InputHdmi3 => tv.ChangeSourceAsync("HDMI 3", cancellationToken),
                Buttons.InputHdmi4 => tv.ChangeSourceAsync("HDMI 4", cancellationToken),
                _ when HisenseDeviceProvider._remoteKeys.TryGetValue(button, out RemoteKey key) => tv.SendKeyAsync(key, cancellationToken),
                _ => Task.CompletedTask
            };
        }
        if (SmartApplicationButton.TryResolve(buttonName) is { } smartButton)
        {
            return smartButton switch
            {
                SmartApplicationButtons.Netflix => tv.LaunchAppAsync("Netflix", cancellationToken),
                SmartApplicationButtons.YouTube => tv.LaunchAppAsync("YouTube", cancellationToken),
                SmartApplicationButtons.GooglePlay => tv.LaunchAppAsync("Play Store", cancellationToken),
                SmartApplicationButtons.Amazon => tv.LaunchAppAsync("Prime Video", cancellationToken),
                _ => Task.CompletedTask
            };
        }
        return buttonName switch
        {
            "HBOMAX" => tv.LaunchAppAsync("HBO Max", cancellationToken),
            _ => Task.CompletedTask
        };
    }

    private Task OnDeviceAdded(string deviceId, CancellationToken cancellationToken)
    {
        this._deviceIds.Add(deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceRemoved(string deviceId, CancellationToken cancellationToken)
    {
        this._deviceIds.Remove(deviceId);
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
            await HisenseTV.TryCreateAsync(
                PhysicalAddress.Parse(optionalDeviceId),
                logger,
                connectionRequired: true,
                useCertificates: true,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false) is { } tv &&
            await HisenseDeviceProvider.AuthenticatedStateAsync(tv, cancellationToken: cancellationToken).ConfigureAwait(false) is { } state)
        {
            this.SetTV(tv, state);
        }
        return (tv = this._tv) is null
            ? []
            : [new(tv.DeviceId, $"Hisense TV ({tv.MacAddress})", true)];
    }

    private async Task<bool> QueryIsRegistered(CancellationToken cancellationToken)
    {
        if (this._tv is { } tv)
        {
            return await HisenseDeviceProvider.AuthenticatedStateAsync(tv, cancellationToken: cancellationToken).ConfigureAwait(false) is not null;
        }
        HisenseTV[] tvs = await HisenseTV.DiscoverAsync(logger, useCertificates: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (tvs.Length == 1 && await HisenseDeviceProvider.AuthenticatedStateAsync(tv = tvs[0], cancellationToken: cancellationToken).ConfigureAwait(false) is { } state)
        {
            this.SetTV(tv, state);
            return true;
        }
        this._candidates = tvs;
        return false;
    }

    private async Task<RegistrationResult> Register(string code, CancellationToken cancellationToken)
    {
        if (code == "0000" && await this.QueryIsRegistered(cancellationToken).ConfigureAwait(false))
        {
            return RegistrationResult.Success;
        }
        HisenseTV[] candidates = this._tv is { } tv ? [tv] : this._candidates;
        for (int index = 0; index < candidates.Length; index++)
        {
            HisenseTV candidate = candidates[index];
            if (await HisenseDeviceProvider.AuthenticatedStateAsync(candidate, code, cancellationToken).ConfigureAwait(false) is { } state)
            {
                this.SetTV(candidate, state);
                if (candidates.Length != 1)
                {
                    // Dispose of the other candidates.
                    for (int other = 0; other < candidates.Length; other++)
                    {
                        if (other != index)
                        {
                            candidates[other].Dispose();
                        }
                    }
                }
                return RegistrationResult.Success;
            }
        }
        return RegistrationResult.Failed("Passcode was either incorrect or you took too long.");
    }

    private async void SetTV(HisenseTV tv, IState? state = default)
    {
        logger.LogInformation("Setting TV to {ipAddress}.", tv.IPAddress);
        this._tv = tv;
        this._connected = tv.IsConnected;
        tv.VolumeChanged += this.TV_VolumeChanged;
        tv.Sleep += this.TV_Disconnected;
        tv.Disconnected += this.TV_Disconnected;
        tv.Connected += this.TV_Connected;
        tv.StateChanged += this.TV_StateChanged;
        if (!this._connected || this._notifier is not { } notifier || !this._deviceIds.Contains(tv.DeviceId))
        {
            return;
        }
        await notifier.SendPowerNotificationAsync(true, tv.DeviceId).ConfigureAwait(false);
        if (state != null)
        {
            await HisenseDeviceProvider.NotifyStateAsync(notifier, tv, state).ConfigureAwait(false);
        }
    }

    private async Task SetVolumeAsync(string deviceId, double value, CancellationToken cancellationToken)
    {
        if (this._tv is { } tv)
        {
            await tv.ChangeVolumeAsync((int)value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async void TV_Connected(object? sender, EventArgs e)
    {
        if (this._tv is not { DeviceId: { } deviceId } || this._notifier is not { } notifier || this._connected)
        {
            return;
        }
        this._connected = true;
        await notifier.SendPowerNotificationAsync(true, deviceId).ConfigureAwait(false);
    }

    private async void TV_Disconnected(object? sender, EventArgs e)
    {
        if (this._tv is not { DeviceId: { } deviceId } || this._notifier is not { } notifier || !this._connected)
        {
            return;
        }
        this._connected = false;
        await notifier.SendPowerNotificationAsync(false, deviceId).ConfigureAwait(false);
    }

    private async void TV_StateChanged(object? sender, DataEventArgs<IState> e)
    {
        if (this._tv is { } tv && this._notifier is { } notifier)
        {
            await HisenseDeviceProvider.NotifyStateAsync(notifier, tv, e.Data).ConfigureAwait(false);
        }
    }

    private async void TV_VolumeChanged(object? sender, DataEventArgs<int> e)
    {
        if (this._tv is { DeviceId: { } deviceId } && this._notifier is { } notifier)
        {
            await Task.WhenAll(
                notifier.SendNotificationAsync("VOLUME", (double)e.Data, deviceId),
                notifier.SendNotificationAsync("VOLUME-LABEL", e.Data.ToString(), deviceId)
            ).ConfigureAwait(false);
        }
    }

    private readonly record struct DeviceTuple(IPAddress IPAddress, PhysicalAddress MacAddress);

    private static class Constants
    {
        public const string DeviceName = "IP Controlled TV";
        public const string Manufacturer = nameof(Hisense);
    }
}
