using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseDeviceProvider : IDeviceProvider, IDisposable
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

    private readonly HisenseClientManager _clientManager;
    private readonly ILogger<HisenseDeviceProvider> _logger;
    private readonly ConcurrentDictionary<string, byte> _subscribedDevices = new(StringComparer.Ordinal);
    private HisenseTV[] _candidates = [];
    private IDeviceNotifier? _notifier;
    private bool _isDisposed;

    public HisenseDeviceProvider(HisenseClientManager clientManager, ILogger<HisenseDeviceProvider> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
        this._clientManager.Registry.Changed += this.Registry_Changed;
        foreach (HisenseTV client in this._clientManager.Clients)
        {
            this.AttachClientEvents(client);
        }
    }

    public IDeviceBuilder DeviceBuilder => field ??= Device.Create(Constants.DeviceName, DeviceType.TV)
        .SetManufacturer(Constants.Manufacturer)
        .SetSpecificName($"{Constants.Manufacturer} {Constants.DeviceName}")
        .AddButtonHandler(this.OnButtonPressedAsync)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.MenuAndBack | ButtonGroups.ControlPad | ButtonGroups.Volume | ButtonGroups.ChannelZapper | ButtonGroups.Transport | ButtonGroups.TransportSearch | ButtonGroups.NumberPad)
        .AddButton(Buttons.InputHdmi1 | Buttons.InputHdmi2 | Buttons.InputHdmi3 | Buttons.InputHdmi4 | Buttons.InputToggle)
        .AddSmartApplicationButton(SmartApplicationButtons.Amazon | SmartApplicationButtons.GooglePlay | SmartApplicationButtons.Netflix | SmartApplicationButtons.YouTube)
        .AddButton(Buttons.Home)
        .AddButton("HBOMAX", "HBO Max")
        .AddDirectory("Apps", default, DirectoryRole.Root, this.BrowseAppsAsync, this.LaunchAppAsync)
        .AddPowerStateSensor(this._clientManager.Registry, this.GetPowerState)
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, this.InitializeDeviceListAsync)
        .EnableNotifications(notifier => this._notifier = notifier)
        .AddSlider("VOLUME", null, (deviceId, _) => Task.FromResult(this.GetVolume(deviceId)), this.SetVolumeAsync)
        .AddTextLabel("VOLUME-LABEL", "Volume", deviceId => this.GetVolume(deviceId).ToString(CultureInfo.InvariantCulture))
        .AddTextLabel("STATE", "State", this.GetState)
        .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync)
        .EnableRegistration(
            "Registering TV...",
            "Enter the code showing on your TV. If no code is displayed, try 0000 or hit back and try again (after verifying the TV is on).",
            this.QueryIsRegisteredAsync,
            this.RegisterAsync
        );

    private static Task<IState?> AuthenticatedStateAsync(HisenseTV tv, string? code = null, CancellationToken cancellationToken = default)
    {
        return code == null
            ? tv.GetStateAsync(cancellationToken).ContinueWith(task => AuthenticatedState(task.Result), TaskContinuationOptions.ExecuteSynchronously)
            : tv.AuthenticateAsync(code, cancellationToken).ContinueWith(task => AuthenticatedState(task.Result), TaskContinuationOptions.ExecuteSynchronously);

        static IState? AuthenticatedState(IState? state) => state is { Type: not StateType.AuthenticationRequired } ? state : default;
    }

    private async Task BrowseAppsAsync(string deviceId, IDirectoryBuilder list, CancellationToken cancellationToken)
    {
        if (!this._clientManager.TryGetConnected(deviceId, out HisenseTV? tv))
        {
            return;
        }
        list.AddTileRow([new("https://logodownload.org/wp-content/uploads/2019/11/hisense-logo.png")]);
        AppInfo[] apps = Array.FindAll(await tv.GetAppsAsync(cancellationToken).ConfigureAwait(false), static app => !app.IsUninstalled);
        Array.Sort(apps, static (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
        (_, int limit, int offset) = list.Parameters;
        if (offset is > 0 && limit < apps.Length)
        {
            int start = offset;
            apps = apps[start..(start + limit)];
        }
        foreach ((string name, string url, _) in apps)
        {
            list.AddEntry(new(name, url, ActionIdentifier: name));
        }
    }

    private bool GetPowerState(string deviceId) => this._clientManager.TryGetConnected(deviceId, out _);

    private double GetVolume(string deviceId) => this._clientManager.TryGetConnected(deviceId, out HisenseTV? tv) ? tv.Volume : 0d;

    private string GetState(string deviceId) => this._clientManager.TryGetConnected(deviceId, out HisenseTV? tv) ? tv.CurrentState?.ToString() ?? string.Empty : string.Empty;

    private Task InitializeDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("{method}:[{devices}]", nameof(this.InitializeDeviceListAsync), string.Join(',', deviceIds));
        foreach (string deviceId in deviceIds)
        {
            this._subscribedDevices.TryAdd(deviceId, default);
            this._clientManager.GetOrCreateKnownClient(deviceId);
        }
        return Task.CompletedTask;
    }

    private Task LaunchAppAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        return this._clientManager.TryGetConnected(deviceId, out HisenseTV? tv)
            ? tv.LaunchAppAsync(actionIdentifier, cancellationToken)
            : Task.CompletedTask;
    }

    private async Task OnButtonPressedAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        if (Button.TryResolve(buttonName) is { } button)
        {
            if (button == Buttons.PowerOn)
            {
                if (this._clientManager.GetClient(deviceId) is { } registeredTv)
                {
                    await registeredTv.MacAddress.WakeAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }
            if (!this._clientManager.TryGetConnected(deviceId, out HisenseTV? tv))
            {
                return;
            }
            Task task = button switch
            {
                Buttons.InputHdmi1 => tv.ChangeSourceAsync("HDMI 1", cancellationToken),
                Buttons.InputHdmi2 => tv.ChangeSourceAsync("HDMI 2", cancellationToken),
                Buttons.InputHdmi3 => tv.ChangeSourceAsync("HDMI 3", cancellationToken),
                Buttons.InputHdmi4 => tv.ChangeSourceAsync("HDMI 4", cancellationToken),
                _ when HisenseDeviceProvider._remoteKeys.TryGetValue(button, out RemoteKey key) => tv.SendKeyAsync(key, cancellationToken),
                _ => Task.CompletedTask
            };
            await task.ConfigureAwait(false);
            return;
        }

        if (!this._clientManager.TryGetConnected(deviceId, out HisenseTV? client))
        {
            return;
        }

        if (SmartApplicationButton.TryResolve(buttonName) is { } smartButton)
        {
            Task task = smartButton switch
            {
                SmartApplicationButtons.Netflix => client.LaunchAppAsync("Netflix", cancellationToken),
                SmartApplicationButtons.YouTube => client.LaunchAppAsync("YouTube", cancellationToken),
                SmartApplicationButtons.GooglePlay => client.LaunchAppAsync("Play Store", cancellationToken),
                SmartApplicationButtons.Amazon => client.LaunchAppAsync("Prime Video", cancellationToken),
                _ => Task.CompletedTask
            };
            await task.ConfigureAwait(false);
            return;
        }

        if (buttonName == "HBOMAX")
        {
            await client.LaunchAppAsync("HBO Max", cancellationToken).ConfigureAwait(false);
        }
    }

    private Task OnDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.TryAdd(deviceId, default);
        return Task.CompletedTask;
    }

    private Task OnDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.TryRemove(deviceId, out _);
        if (this._clientManager.Registry.TryRemove(deviceId, out HisenseTV? tv))
        {
            tv.Dispose();
        }
        return Task.CompletedTask;
    }

    private async Task<DiscoveredDevice[]> PerformDiscoveryAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        if (optionalDeviceId != null &&
            this._clientManager.GetOrCreateKnownClient(optionalDeviceId) is { } tv &&
            await HisenseDeviceProvider.AuthenticatedStateAsync(tv, cancellationToken: cancellationToken).ConfigureAwait(false) is not null)
        {
            return [new(tv.DeviceId, $"Hisense TV ({tv.MacAddress})", true)];
        }
        HisenseTV[] discovered = await this._clientManager.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return [.. discovered.Select(static tv => new DiscoveredDevice(tv.DeviceId, $"Hisense TV ({tv.MacAddress})", true))];
        }
        finally
        {
            // Discovery only reports; nothing is registered until the user pairs a TV.
            this._clientManager.Release(discovered);
        }
    }

    private async Task<bool> QueryIsRegisteredAsync(CancellationToken cancellationToken)
    {
        HisenseTV[] candidates = await this.GatherCandidatesAsync(cancellationToken).ConfigureAwait(false);
        foreach (HisenseTV candidate in candidates)
        {
            if (await HisenseDeviceProvider.AuthenticatedStateAsync(candidate, cancellationToken: cancellationToken).ConfigureAwait(false) is not null)
            {
                this.AdoptCandidate(candidate);
                return true;
            }
        }
        return false;
    }

    private async Task<RegistrationResult> RegisterAsync(string code, CancellationToken cancellationToken)
    {
        if (code == "0000" && await this.QueryIsRegisteredAsync(cancellationToken).ConfigureAwait(false))
        {
            return RegistrationResult.Success;
        }
        HisenseTV[] candidates = this._candidates.Length > 0 ? this._candidates : await this.GatherCandidatesAsync(cancellationToken).ConfigureAwait(false);
        foreach (HisenseTV candidate in candidates)
        {
            if (await HisenseDeviceProvider.AuthenticatedStateAsync(candidate, code, cancellationToken).ConfigureAwait(false) is not null)
            {
                this.AdoptCandidate(candidate);
                return RegistrationResult.Success;
            }
        }
        return RegistrationResult.Failed("Passcode was either incorrect or you took too long.");
    }

    /// <summary>
    /// Collects TVs that could be paired: known clients not yet added as devices, or - if there are none - freshly discovered ones.
    /// Previously gathered candidates that were never adopted are released.
    /// </summary>
    private async Task<HisenseTV[]> GatherCandidatesAsync(CancellationToken cancellationToken)
    {
        this._clientManager.Release(Interlocked.Exchange(ref this._candidates, []));
        HisenseTV[] candidates = [.. this._clientManager.Clients.Where(client => !this._subscribedDevices.ContainsKey(client.DeviceId))];
        if (candidates.Length == 0)
        {
            candidates = [.. (await this._clientManager.DiscoverAsync(cancellationToken).ConfigureAwait(false)).Where(client => !this._subscribedDevices.ContainsKey(client.DeviceId))];
        }
        this._candidates = candidates;
        return candidates;
    }

    private void AdoptCandidate(HisenseTV candidate)
    {
        HisenseTV[] others = [.. Interlocked.Exchange(ref this._candidates, []).Where(other => !ReferenceEquals(other, candidate))];
        this._clientManager.Adopt(candidate);
        this._clientManager.Release(others);
    }

    private async Task SetVolumeAsync(string deviceId, double value, CancellationToken cancellationToken)
    {
        if (this._clientManager.TryGetConnected(deviceId, out HisenseTV? tv))
        {
            await tv.ChangeVolumeAsync((int)value, cancellationToken).ConfigureAwait(false);
        }
    }

    private void Registry_Changed(object? sender, DeviceRegistryChangedEventArgs<HisenseTV> e)
    {
        if (e.Kind == DeviceRegistryChangeKind.Removed)
        {
            this.DetachClientEvents(e.Client);
        }
        else if (e.Kind == DeviceRegistryChangeKind.Added)
        {
            this.AttachClientEvents(e.Client);
        }
    }

    private void AttachClientEvents(HisenseTV client)
    {
        this.DetachClientEvents(client);
        client.VolumeChanged += this.Client_VolumeChanged;
        client.StateChanged += this.Client_StateChanged;
    }

    private void DetachClientEvents(HisenseTV client)
    {
        client.VolumeChanged -= this.Client_VolumeChanged;
        client.StateChanged -= this.Client_StateChanged;
    }

    private async void Client_VolumeChanged(object? sender, DataEventArgs<int> e)
    {
        if (sender is HisenseTV tv && this._subscribedDevices.ContainsKey(tv.DeviceId) && this._notifier is { } notifier)
        {
            await this.NotifyAsync(() => Task.WhenAll(
                notifier.SendNotificationAsync("VOLUME", (double)e.Data, tv.DeviceId),
                notifier.SendNotificationAsync("VOLUME-LABEL", e.Data.ToString(CultureInfo.InvariantCulture), tv.DeviceId)
            )).ConfigureAwait(false);
        }
    }

    private async void Client_StateChanged(object? sender, DataEventArgs<IState> e)
    {
        if (sender is HisenseTV tv && this._subscribedDevices.ContainsKey(tv.DeviceId) && this._notifier is { } notifier)
        {
            await this.NotifyAsync(() => notifier.SendNotificationAsync("STATE", e.Data.ToString(), tv.DeviceId)).ConfigureAwait(false);
        }
    }

    // Event handlers are async void, so failures must be caught here or they would crash the process.
    private async Task NotifyAsync(Func<Task> send)
    {
        try
        {
            await send().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to send notification.");
        }
    }

    public void Dispose()
    {
        if (this._isDisposed)
        {
            return;
        }
        this._isDisposed = true;
        this._clientManager.Registry.Changed -= this.Registry_Changed;
        foreach (HisenseTV client in this._clientManager.Clients)
        {
            this.DetachClientEvents(client);
        }
    }

    private static class Constants
    {
        public const string DeviceName = "IP Controlled TV";
        public const string Manufacturer = nameof(Hisense);
    }
}
