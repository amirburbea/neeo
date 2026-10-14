using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.WebOS.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.WebOS;

public sealed class WebOSDeviceProvider : IDeviceProvider, IDisposable
{
    private const string DeviceName = "WebOS TV";
    private const string Manufacturer = "LG";

    private readonly WebOSClientManager _clientManager;
    private readonly ILogger<WebOSDeviceProvider> _logger;
    // Device IDs the Brain has added for this adapter (written from the Brain's subscription callbacks, read from client events).
    private readonly ConcurrentDictionary<string, byte> _subscribedDevices = new(StringComparer.Ordinal);
    private IDeviceNotifier? _notifier;
    private bool _isDisposed;

    public WebOSDeviceProvider(WebOSClientManager clientManager, ILogger<WebOSDeviceProvider> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
        this._clientManager.Registry.Changed += this.Registry_Changed;
        foreach (WebOSClient client in this._clientManager.Clients)
        {
            this.AttachClientEvents(client);
        }
    }

    public IDeviceBuilder DeviceBuilder => field ??= Device.Create(DeviceName, DeviceType.TV)
        .SetManufacturer(Manufacturer)
        .SetSpecificName($"{Manufacturer} {DeviceName}")
        .AddAdditionalSearchTokens("webOS", "LG", "OLED")
        .AddButtonHandler(this.OnButtonPressedAsync)
        .AddButtonGroup(
            ButtonGroups.Power |
            ButtonGroups.MenuAndBack |
            ButtonGroups.ControlPad |
            ButtonGroups.Volume |
            ButtonGroups.ChannelZapper |
            ButtonGroups.Transport |
            ButtonGroups.TransportSearch |
            ButtonGroups.NumberPad
        )
        .AddButton(
            Buttons.InputHdmi1 |
            Buttons.InputHdmi2 |
            Buttons.InputHdmi3 |
            Buttons.InputHdmi4 |
            Buttons.InputToggle |
            Buttons.Home |
            Buttons.FunctionRed |
            Buttons.FunctionGreen |
            Buttons.FunctionYellow |
            Buttons.FunctionBlue |
            Buttons.Info |
            Buttons.Guide |
            Buttons.Exit
        )
        .AddSmartApplicationButton(
            SmartApplicationButtons.Amazon |
            SmartApplicationButtons.GooglePlay |
            SmartApplicationButtons.Netflix |
            SmartApplicationButtons.YouTube
        )
        .AddDirectory("Apps", "Apps & Inputs", DirectoryRole.Root, this.BrowseAppsAsync, this.LaunchAppAsync)
        .AddPowerStateSensor(this._clientManager.Registry, (deviceId, _) => Task.FromResult(this.GetPowerState(deviceId)))
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, this.InitializeDeviceListAsync)
        .EnableNotifications(notifier => this._notifier = notifier)
        .AddSlider("VOLUME", null, (deviceId, _) => Task.FromResult(this.GetVolume(deviceId)), this.SetVolumeAsync)
        .AddTextLabel("VOLUME-LABEL", "Volume", deviceId => this.GetVolume(deviceId).ToString(CultureInfo.InvariantCulture))
        .AddTextLabel("APP", "Current App", this.GetForegroundApp)
        .EnableDiscovery(
            "Discovering LG webOS TV...",
            "Ensure your TV is turned on, connected to the local network, and 'LG Connect Apps' is enabled. To turn the TV on from NEEO, also enable 'Turn on via Wi-Fi' (or 'Turn on via LAN').",
            this.PerformDiscoveryAsync
        )
        .EnableRegistration(
            "Pairing with LG webOS TV...",
            "Turn your TV on, enter any code and submit. A prompt will then appear on your TV screen asking to allow NEEO Remote - click 'Allow' with your LG remote within 60 seconds.",
            this.QueryIsRegisteredAsync,
            this.RegisterAsync
        );

    private bool GetPowerState(string deviceId)
    {
        if (this._clientManager.TryGetConnected(deviceId, out _))
        {
            return true;
        }
        // The Brain asking is a good moment to retry (coalesced, and restarts the backoff) - e.g. the TV was just turned on.
        this._clientManager.GetClient(deviceId)?.RequestReconnect();
        return false;
    }

    private double GetVolume(string deviceId)
    {
        return this._clientManager.GetClient(deviceId) is { } client ? client.Volume : 0d;
    }

    private async Task SetVolumeAsync(string deviceId, double value, CancellationToken cancellationToken)
    {
        if (this._clientManager.TryGetConnected(deviceId, out WebOSClient? client))
        {
            await client.SetVolumeAsync((int)value, cancellationToken).ConfigureAwait(false);
        }
    }

    private string GetForegroundApp(string deviceId)
    {
        return this._clientManager.GetClient(deviceId)?.ForegroundApp ?? string.Empty;
    }

    private async Task OnButtonPressedAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        WebOSClient? client = this._clientManager.GetClient(deviceId);
        if (client == null)
        {
            return;
        }

        Buttons? resolved = Button.TryResolve(buttonName);
        if (resolved is Buttons.PowerOn || (resolved is Buttons.PowerToggle && !client.IsConnected))
        {
            await client.PowerOnAsync(cancellationToken).ConfigureAwait(false);
            return;
        }
        if (!client.IsConnected)
        {
            this._logger.LogInformation("Can not process {button} as the TV is not connected.", buttonName);
            client.RequestReconnect();
            return;
        }

        if (resolved is { } button)
        {
            switch (button)
            {
                case Buttons.PowerOff or Buttons.PowerToggle:
                    await client.PowerOffAsync(cancellationToken).ConfigureAwait(false);
                    return;
                case Buttons.InputHdmi1:
                    await client.LaunchAppAsync("com.webos.app.hdmi1", cancellationToken).ConfigureAwait(false);
                    return;
                case Buttons.InputHdmi2:
                    await client.LaunchAppAsync("com.webos.app.hdmi2", cancellationToken).ConfigureAwait(false);
                    return;
                case Buttons.InputHdmi3:
                    await client.LaunchAppAsync("com.webos.app.hdmi3", cancellationToken).ConfigureAwait(false);
                    return;
                case Buttons.InputHdmi4:
                    await client.LaunchAppAsync("com.webos.app.hdmi4", cancellationToken).ConfigureAwait(false);
                    return;
                default:
                    await client.SendKeyAsync(button, cancellationToken).ConfigureAwait(false);
                    return;
            }
        }

        if (SmartApplicationButton.TryResolve(buttonName) is { } smartButton)
        {
            string? appId = smartButton switch
            {
                SmartApplicationButtons.Netflix => "netflix",
                SmartApplicationButtons.YouTube => "youtube.leanback.v4",
                SmartApplicationButtons.Amazon => "amazon",
                SmartApplicationButtons.GooglePlay => "googleplaymovieswebos",
                _ => null
            };

            if (appId != null)
            {
                await client.LaunchAppAsync(appId, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task BrowseAppsAsync(string deviceId, IDirectoryBuilder list, CancellationToken cancellationToken)
    {
        if (!this._clientManager.TryGetConnected(deviceId, out WebOSClient? client))
        {
            return;
        }

        // Responses are matched to requests by id, so the two queries can be in flight together.
        WebOSAppInfo[][] results = await Task.WhenAll(
            client.GetAppsAsync(cancellationToken),
            client.GetExternalInputsAsync(cancellationToken)
        ).ConfigureAwait(false);
        (WebOSAppInfo[] apps, WebOSAppInfo[] inputs) = (results[0], results[1]);

        List<WebOSAppInfo> allItems = [.. inputs, .. apps.Where(app => !app.Id.StartsWith("com.webos.app.hdmi", StringComparison.Ordinal))];

        foreach (WebOSAppInfo item in allItems)
        {
            list.AddEntry(new(item.Title, item.Icon, ActionIdentifier: item.Id));
        }
    }

    private async Task LaunchAppAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        if (this._clientManager.TryGetConnected(deviceId, out WebOSClient? client))
        {
            await client.LaunchAppAsync(actionIdentifier, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<DiscoveredDevice[]> PerformDiscoveryAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        if (optionalDeviceId != null && PhysicalAddress.TryParse(optionalDeviceId, out PhysicalAddress? mac))
        {
            if (this._clientManager.GetClient(optionalDeviceId) is { } existing)
            {
                return [new(existing.DeviceId, $"{DeviceName} ({existing.IPAddress})", true)];
            }
        }

        WebOSClient[] clients = await this._clientManager.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        return [.. clients.Select(static c => new DiscoveredDevice(c.DeviceId, $"{DeviceName} ({c.IPAddress}){(c.IsPaired ? string.Empty : " - not paired")}", true))];
    }

    private Task<bool> QueryIsRegisteredAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(this._clientManager.HasPairedDevices);
    }

    // The NEEO app registers before it discovers, so this is where TVs get paired. The security code itself is not used.
    private async Task<RegistrationResult> RegisterAsync(string securityCode, CancellationToken cancellationToken)
    {
        return await this._clientManager.PairAsync(cancellationToken).ConfigureAwait(false)
            ? RegistrationResult.Success
            : RegistrationResult.Failed("No TV was paired - make sure it is on, then accept the prompt on the TV within 60 seconds.");
    }

    private Task OnDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.TryAdd(deviceId, default);
        this._clientManager.GetOrCreateKnownClient(deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.TryRemove(deviceId, out _);
        if (this._clientManager.Registry.TryRemove(deviceId, out WebOSClient? client))
        {
            client.Dispose();
        }
        return Task.CompletedTask;
    }

    private Task InitializeDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken)
    {
        foreach (string deviceId in deviceIds)
        {
            this._subscribedDevices.TryAdd(deviceId, default);
            // Starts connecting in the background (a TV that is off is retried, and picked up when it comes on).
            this._clientManager.GetOrCreateKnownClient(deviceId);
        }
        return Task.CompletedTask;
    }

    // Every client's events are observed for as long as it is registered; the handlers only notify for devices the Brain has
    // added, so the order in which clients and subscriptions arrive does not matter.
    private void Registry_Changed(object? sender, DeviceRegistryChangedEventArgs<WebOSClient> e)
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

    private void AttachClientEvents(WebOSClient client)
    {
        // Detach first so attaching twice (constructor and Added racing) never subscribes twice.
        this.DetachClientEvents(client);
        client.VolumeChanged += this.Client_VolumeChanged;
        client.ForegroundAppChanged += this.Client_ForegroundAppChanged;
    }

    private void DetachClientEvents(WebOSClient client)
    {
        client.VolumeChanged -= this.Client_VolumeChanged;
        client.ForegroundAppChanged -= this.Client_ForegroundAppChanged;
    }

    private void Client_VolumeChanged(object? sender, DataEventArgs<int> e)
    {
        if (sender is WebOSClient client && this._subscribedDevices.ContainsKey(client.DeviceId) && this._notifier is { } notifier)
        {
            _ = notifier.SendNotificationAsync("VOLUME", (double)e.Data, client.DeviceId);
            _ = notifier.SendNotificationAsync("VOLUME-LABEL", e.Data.ToString(CultureInfo.InvariantCulture), client.DeviceId);
        }
    }

    private void Client_ForegroundAppChanged(object? sender, DataEventArgs<string> e)
    {
        if (sender is WebOSClient client && this._subscribedDevices.ContainsKey(client.DeviceId) && this._notifier is { } notifier)
        {
            _ = notifier.SendNotificationAsync("APP", e.Data, client.DeviceId);
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
        foreach (WebOSClient client in this._clientManager.Clients)
        {
            this.DetachClientEvents(client);
        }
    }
}
