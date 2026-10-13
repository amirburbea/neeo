using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    private readonly HashSet<string> _subscribedDevices = new(StringComparer.Ordinal);
    private IDeviceNotifier? _notifier;
    private bool _isDisposed;

    public WebOSDeviceProvider(WebOSClientManager clientManager, ILogger<WebOSDeviceProvider> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
        this._clientManager.Registry.Changed += this.Registry_Changed;
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
        .AddPowerStateSensor(this._clientManager.Registry, (deviceId, _) => this.GetPowerStateAsync(deviceId))
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAddedAsync, this.OnDeviceRemovedAsync, this.InitializeDeviceListAsync)
        .EnableNotifications(notifier => this._notifier = notifier)
        .AddSlider("VOLUME", null, this.GetVolumeAsync, this.SetVolumeAsync)
        .AddTextLabel("VOLUME-LABEL", "Volume", async (deviceId, ct) => (await this.GetVolumeAsync(deviceId, ct).ConfigureAwait(false)).ToString())
        .AddTextLabel("APP", "Current App", this.GetForegroundAppAsync)
        .EnableDiscovery(
            "Discovering LG webOS TV...",
            "Ensure your TV is turned on, connected to the local network, and 'LG Connect Apps' is enabled.",
            this.PerformDiscoveryAsync
        )
        .EnableRegistration(
            "Pairing with LG webOS TV...",
            "A prompt will appear on your TV screen asking to allow NEEO Remote. Click 'Allow' with your LG remote, then submit to finish pairing.",
            this.QueryIsRegisteredAsync,
            this.RegisterAsync
        );

    private Task<bool> GetPowerStateAsync(string deviceId)
    {
        return Task.FromResult(this._clientManager.TryGetConnected(deviceId, out _));
    }

    private Task<double> GetVolumeAsync(string deviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(this._clientManager.GetClient(deviceId) is { } client ? (double)client.Volume : 0d);
    }

    private async Task SetVolumeAsync(string deviceId, double value, CancellationToken cancellationToken)
    {
        if (this._clientManager.TryGetConnected(deviceId, out WebOSClient? client))
        {
            await client.SetVolumeAsync((int)value, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<string> GetForegroundAppAsync(string deviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(this._clientManager.GetClient(deviceId)?.ForegroundApp ?? string.Empty);
    }

    private async Task OnButtonPressedAsync(string deviceId, string buttonName, CancellationToken cancellationToken)
    {
        WebOSClient? client = this._clientManager.GetClient(deviceId);
        if (client == null)
        {
            return;
        }

        if (Button.TryResolve(buttonName) is { } button)
        {
            switch (button)
            {
                case Buttons.PowerOn:
                    await client.PowerOnAsync(cancellationToken).ConfigureAwait(false);
                    return;
                case Buttons.PowerOff:
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

        WebOSAppInfo[] apps = await client.GetAppsAsync(cancellationToken).ConfigureAwait(false);
        WebOSAppInfo[] inputs = await client.GetExternalInputsAsync(cancellationToken).ConfigureAwait(false);

        List<WebOSAppInfo> allItems = [.. inputs, .. apps.Where(app => !app.Id.StartsWith("com.webos.app.hdmi", StringComparison.OrdinalIgnoreCase))];

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
        return [.. clients.Select(c => new DiscoveredDevice(c.DeviceId, $"{DeviceName} ({c.IPAddress})", true))];
    }

    private Task<bool> QueryIsRegisteredAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(this._clientManager.Clients.Any(static c => c.IsConnected && !string.IsNullOrEmpty(c.ClientKey)));
    }

    private async Task<RegistrationResult> RegisterAsync(string securityCode, CancellationToken cancellationToken)
    {
        foreach (WebOSClient client in this._clientManager.Clients)
        {
            if (client.IsConnected)
            {
                return RegistrationResult.Success;
            }
            if (await client.ConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                return RegistrationResult.Success;
            }
        }
        return RegistrationResult.Failed("Pairing timed out or was rejected by the TV.");
    }

    private Task OnDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.Add(deviceId);
        if (this._clientManager.GetClient(deviceId) is { } client)
        {
            this.AttachClientEvents(client);
        }
        return Task.CompletedTask;
    }

    private Task OnDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.Remove(deviceId);
        if (this._clientManager.Registry.TryRemove(deviceId, out WebOSClient? client))
        {
            this.DetachClientEvents(client);
            client.Dispose();
        }
        return Task.CompletedTask;
    }

    private async Task InitializeDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken)
    {
        foreach (string deviceId in deviceIds)
        {
            this._subscribedDevices.Add(deviceId);
            if (PhysicalAddress.TryParse(deviceId, out PhysicalAddress? mac))
            {
                if (NetworkMethods.GetNetworkDevices().FirstOrDefault(d => d.Value.Equals(mac)) is { Key: { } ipAddress })
                {
                    await this._clientManager.GetOrCreateClientAsync(ipAddress, mac, 3000, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private void Registry_Changed(object? sender, DeviceRegistryChangedEventArgs<WebOSClient> e)
    {
        if (e.Kind == DeviceRegistryChangeKind.Removed)
        {
            this.DetachClientEvents(e.Client);
        }
        else if (e.Kind == DeviceRegistryChangeKind.Added && this._subscribedDevices.Contains(e.DeviceId))
        {
            this.AttachClientEvents(e.Client);
        }
    }

    private void AttachClientEvents(WebOSClient client)
    {
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
        if (sender is WebOSClient client && this._notifier is { } notifier)
        {
            _ = notifier.SendNotificationAsync("VOLUME", (double)e.Data, client.DeviceId);
            _ = notifier.SendNotificationAsync("VOLUME-LABEL", e.Data.ToString(), client.DeviceId);
        }
    }

    private void Client_ForegroundAppChanged(object? sender, DataEventArgs<string> e)
    {
        if (sender is WebOSClient client && this._notifier is { } notifier)
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
