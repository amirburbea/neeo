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
        this._clientManager.ClientAdded += this.ClientManager_ClientAdded;
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
        .AddPowerStateSensor(this.GetPowerState)
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
            _ => Task.FromResult(this._clientManager.HasPairedDevices),
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
        List<WebOSAppInfo> allItems = [.. inputs, .. apps.Where(static app => !app.Id.StartsWith("com.webos.app.hdmi", StringComparison.Ordinal))];
        list.AddPageEntries(allItems, static item => new(item.Title, item.Icon, ActionIdentifier: item.Id));
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

    // The NEEO app registers before it discovers, so this is where TVs get paired. The security code itself is not used.
    private async Task<RegistrationResult> RegisterAsync(string securityCode, CancellationToken cancellationToken)
    {
        return await this._clientManager.PairAsync(cancellationToken).ConfigureAwait(false)
            ? RegistrationResult.Success
            : RegistrationResult.Failed("No TV was paired - make sure it is on, then accept the prompt on the TV within 60 seconds.");
    }

    internal async Task OnDeviceAddedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.TryAdd(deviceId, default);
        await this.NotifyIfConnectedAsync(this._clientManager.GetOrCreateKnownClient(deviceId)).ConfigureAwait(false);
    }

    private Task OnDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken)
    {
        this._subscribedDevices.TryRemove(deviceId, out _);
        if (this._clientManager.TryRemove(deviceId, out WebOSClient? client))
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
            this._subscribedDevices.TryAdd(deviceId, default);
            // Starts connecting in the background (a TV that is off is retried, and picked up when it comes on).
            await this.NotifyIfConnectedAsync(this._clientManager.GetOrCreateKnownClient(deviceId)).ConfigureAwait(false);
        }
    }

    // Every client's events are observed from the moment it is created; the handlers only notify for devices the Brain has
    // added, so the order in which clients and subscriptions arrive does not matter.
    private void ClientManager_ClientAdded(object? sender, DataEventArgs<WebOSClient> e) => this.AttachClientEvents(e.Data);

    // A TV that is already connected when the Brain adds it will not report a change.
    private Task NotifyIfConnectedAsync(WebOSClient? client) => client is { IsConnected: true } ? this.HandleConnectionChangedAsync(client, true) : Task.CompletedTask;

    private void AttachClientEvents(WebOSClient client)
    {
        // Detach first so attaching twice (constructor and Added racing) never subscribes twice.
        this.DetachClientEvents(client);
        client.Connected += this.Client_Connected;
        client.Disconnected += this.Client_Disconnected;
        client.VolumeChanged += this.Client_VolumeChanged;
        client.ForegroundAppChanged += this.Client_ForegroundAppChanged;
    }

    private void DetachClientEvents(WebOSClient client)
    {
        client.Connected -= this.Client_Connected;
        client.Disconnected -= this.Client_Disconnected;
        client.VolumeChanged -= this.Client_VolumeChanged;
        client.ForegroundAppChanged -= this.Client_ForegroundAppChanged;
    }

    private void Client_Connected(object? sender, EventArgs e) => _ = this.HandleConnectionChangedAsync(sender, true);

    private void Client_Disconnected(object? sender, EventArgs e) => _ = this.HandleConnectionChangedAsync(sender, false);

    internal async Task HandleConnectionChangedAsync(object? sender, bool isConnected)
    {
        if (sender is WebOSClient client && this._subscribedDevices.ContainsKey(client.DeviceId) && this._notifier is { } notifier)
        {
            await this.NotifyAsync(() => notifier.SendPowerNotificationAsync(isConnected, client.DeviceId)).ConfigureAwait(false);
        }
    }

    private void Client_VolumeChanged(object? sender, DataEventArgs<int> e) => _ = this.HandleVolumeChangedAsync(sender, e.Data);

    internal async Task HandleVolumeChangedAsync(object? sender, int volume)
    {
        if (sender is WebOSClient client && this._subscribedDevices.ContainsKey(client.DeviceId) && this._notifier is { } notifier)
        {
            await this.NotifyAsync(() => Task.WhenAll(
                notifier.SendNotificationAsync("VOLUME", (double)volume, client.DeviceId),
                notifier.SendNotificationAsync("VOLUME-LABEL", volume.ToString(CultureInfo.InvariantCulture), client.DeviceId)
            )).ConfigureAwait(false);
        }
    }

    private void Client_ForegroundAppChanged(object? sender, DataEventArgs<string> e) => _ = this.HandleForegroundAppChangedAsync(sender, e.Data);

    internal async Task HandleForegroundAppChangedAsync(object? sender, string appId)
    {
        if (sender is WebOSClient client && this._subscribedDevices.ContainsKey(client.DeviceId) && this._notifier is { } notifier)
        {
            await this.NotifyAsync(() => notifier.SendNotificationAsync("APP", appId, client.DeviceId)).ConfigureAwait(false);
        }
    }

    private async Task NotifyAsync(Func<Task> send)
    {
        try
        {
            await send().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to send webOS notification.");
        }
    }

    public void Dispose()
    {
        if (this._isDisposed)
        {
            return;
        }
        this._isDisposed = true;
        this._clientManager.ClientAdded -= this.ClientManager_ClientAdded;
        foreach (WebOSClient client in this._clientManager.Clients)
        {
            this.DetachClientEvents(client);
        }
    }
}
