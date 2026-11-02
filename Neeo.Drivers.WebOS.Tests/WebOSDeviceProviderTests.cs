using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Drivers.WebOS.Models;
using Neeo.Sdk;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.WebOS.Tests;

public sealed class WebOSDeviceProviderTests
{
    [Fact]
    public void DeviceBuilder_configures_expected_capabilities()
    {
        Mock<ISettingsStore<WebOSClientManager>> settingsMock = new();
        WebOSClientManager manager = new(
            Mock.Of<IBrain>(b => b.ServiceEndPoint == new IPEndPoint(IPAddress.Loopback, 3000)),
            NullLogger<WebOSClient>.Instance,
            NullLogger<WebOSClientManager>.Instance,
            settingsMock.Object
        );
        WebOSDeviceProvider provider = new(manager, NullLogger<WebOSDeviceProvider>.Instance);

        IDeviceBuilder builder = provider.DeviceBuilder;

        Assert.NotNull(builder);
        Assert.Equal("WebOS TV", builder.Name);
        Assert.Equal("LG", builder.Manufacturer);
        Assert.True(builder.HasPowerStateSensor);
        Assert.Contains("Apps", builder.Directories);
    }

    [Theory]
    [InlineData(Buttons.CursorUp, "UP")]
    [InlineData(Buttons.CursorDown, "DOWN")]
    [InlineData(Buttons.CursorLeft, "LEFT")]
    [InlineData(Buttons.CursorRight, "RIGHT")]
    [InlineData(Buttons.CursorEnter, "ENTER")]
    [InlineData(Buttons.Back, "BACK")]
    [InlineData(Buttons.Home, "HOME")]
    [InlineData(Buttons.Menu, "MENU")]
    [InlineData(Buttons.FunctionRed, "RED")]
    [InlineData(Buttons.FunctionGreen, "GREEN")]
    [InlineData(Buttons.FunctionYellow, "YELLOW")]
    [InlineData(Buttons.FunctionBlue, "BLUE")]
    [InlineData(Buttons.VolumeUp, "VOLUMEUP")]
    [InlineData(Buttons.VolumeDown, "VOLUMEDOWN")]
    [InlineData(Buttons.MuteToggle, "MUTE")]
    public void WebOSRemoteKey_maps_buttons_correctly(Buttons button, string expectedKey)
    {
        Assert.True(WebOSRemoteKey.TryGetButtonName(button, out string actualKey));
        Assert.Equal(expectedKey, actualKey);
    }

    [Fact]
    public void WebOSResponse_deserializes_correctly()
    {
        const string json = """
        {
            "id": "sub_volume",
            "type": "response",
            "payload": {
                "returnValue": true,
                "volume": 35,
                "muted": false
            }
        }
        """;

        WebOSResponse response = JsonSerializer.Deserialize<WebOSResponse>(json, WebOSJsonSerializerOptions.Default);

        Assert.Equal("sub_volume", response.Id);
        Assert.Equal("response", response.Type);
        Assert.True(response.Payload.GetProperty("returnValue").GetBoolean());
        Assert.Equal(35, response.Payload.GetProperty("volume").GetInt32());
        Assert.False(response.Payload.GetProperty("muted").GetBoolean());
    }

    [Fact]
    public void WebOSAppInfo_deserializes_correctly()
    {
        const string json = """
        [
            { "id": "netflix", "title": "Netflix", "icon": "http://tv/netflix.png" },
            { "id": "youtube", "title": "YouTube", "icon": "http://tv/youtube.png" }
        ]
        """;

        WebOSAppInfo[]? apps = JsonSerializer.Deserialize<WebOSAppInfo[]>(json, WebOSJsonSerializerOptions.Default);

        Assert.NotNull(apps);
        Assert.Equal(2, apps.Length);
        Assert.Equal("netflix", apps[0].Id);
        Assert.Equal("Netflix", apps[0].Title);
        Assert.Equal("http://tv/netflix.png", apps[0].Icon);
    }

    [Fact]
    public void A_new_client_is_identified_by_its_mac_address_and_starts_disconnected()
    {
        IPAddress ip = IPAddress.Parse("192.168.1.50");
        PhysicalAddress mac = PhysicalAddress.Parse("001122334455");
        using WebOSClient client = new(ip, mac, NullLogger.Instance);

        Assert.Equal(mac.ToString(), client.DeviceId);
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task Unpaired_client_does_not_connect_or_reconnect()
    {
        using WebOSClient client = new(IPAddress.Loopback, PhysicalAddress.Parse("001122334455"), NullLogger.Instance);

        Assert.False(client.IsPaired);
        Assert.False(await client.ConnectAsync(TestContext.Current.CancellationToken));
        client.RequestReconnect(); // No-op while unpaired.
        Assert.False(client.IsConnected);
    }

    [Fact]
    public void Client_with_a_stored_key_is_paired()
    {
        using WebOSClient client = new(IPAddress.Loopback, PhysicalAddress.Parse("001122334455"), NullLogger.Instance, clientKey: "key");

        Assert.True(client.IsPaired);
        Assert.Equal("key", client.ClientKey);
    }

    [Fact]
    public void GetOrCreateClient_creates_one_client_per_device_and_remembers_its_address()
    {
        InMemorySettings settings = new();
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);
        PhysicalAddress mac = PhysicalAddress.Parse("001122334455");

        WebOSClient first = manager.GetOrCreateClient(IPAddress.Parse("192.0.2.10"), mac);
        WebOSClient second = manager.GetOrCreateClient(IPAddress.Parse("192.0.2.10"), mac);

        Assert.Same(first, second);
        Assert.Same(first, manager.GetClient(mac.ToString()));
        Assert.Equal("192.0.2.10", settings[$"{mac}|ip"]);
        Assert.False(manager.HasPairedDevices);
    }

    [Fact]
    public void Clients_are_created_with_their_stored_pairing_key()
    {
        PhysicalAddress mac = PhysicalAddress.Parse("001122334455");
        InMemorySettings settings = new() { [mac.ToString()] = "stored-key" };
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);

        WebOSClient client = manager.GetOrCreateClient(IPAddress.Parse("192.0.2.10"), mac);

        Assert.True(manager.HasPairedDevices);
        Assert.Equal("stored-key", client.ClientKey);
    }

    [Fact]
    public void GetOrCreateKnownClient_falls_back_to_the_last_known_address()
    {
        // A MAC address that won't be in this machine's ARP table.
        PhysicalAddress mac = PhysicalAddress.Parse("02DEADBEEF01");
        InMemorySettings settings = new() { [mac.ToString()] = "stored-key", [$"{mac}|ip"] = "192.0.2.20" };
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);

        WebOSClient? client = manager.GetOrCreateKnownClient(mac.ToString());

        Assert.NotNull(client);
        Assert.Equal(IPAddress.Parse("192.0.2.20"), client.IPAddress);
        Assert.Null(manager.GetOrCreateKnownClient("02DEADBEEF02"));
    }

    [Fact]
    public async Task Volume_and_app_changes_send_notifications()
    {
        PhysicalAddress mac = PhysicalAddress.Parse("001122334455");
        InMemorySettings settings = new();
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);
        using WebOSDeviceProvider provider = new(manager, NullLogger<WebOSDeviceProvider>.Instance);
        WebOSClient client = manager.GetOrCreateClient(IPAddress.Loopback, mac);
        RecordingNotifier notifier = new();
        provider.DeviceBuilder.NotifierCallback!(notifier);
        await provider.OnDeviceAddedAsync(client.DeviceId, TestContext.Current.CancellationToken);

        // The current values are not re-sent when subscribing.
        Assert.DoesNotContain(notifier.Sent, entry => entry.Component is "VOLUME" or "VOLUME-LABEL" or "APP");

        client.Volume = 25;
        client.Volume = 25;
        client.ForegroundApp = "netflix";

        Assert.Equal(1, notifier.Sent.Count(entry => entry is ("VOLUME", 25d, _)));
        Assert.Equal(1, notifier.Sent.Count(entry => entry is ("VOLUME-LABEL", "25", _)));
        Assert.Equal(1, notifier.Sent.Count(entry => entry is ("APP", "netflix", _)));
        Assert.All(notifier.Sent, entry => Assert.Equal(client.DeviceId, entry.DeviceId));
    }

    [Fact]
    public async Task Connection_changes_send_power_notifications()
    {
        InMemorySettings settings = new();
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);
        using WebOSDeviceProvider provider = new(manager, NullLogger<WebOSDeviceProvider>.Instance);
        WebOSClient client = manager.GetOrCreateClient(IPAddress.Parse("10.0.0.1"), PhysicalAddress.Parse("001122334455"));
        RecordingNotifier notifier = new();
        provider.DeviceBuilder.NotifierCallback!(notifier);
        await provider.OnDeviceAddedAsync(client.DeviceId, TestContext.Current.CancellationToken);

        client.PublishConnected(true);
        client.PublishConnected(false);

        // The initial (disconnected) state is sent on subscribing.
        Assert.Equal(new List<bool> { false, true, false }, notifier.PowerStates);
    }

    [Fact]
    public async Task Removing_a_device_stops_notifications()
    {
        InMemorySettings settings = new();
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);
        using WebOSDeviceProvider provider = new(manager, NullLogger<WebOSDeviceProvider>.Instance);
        WebOSClient client = manager.GetOrCreateClient(IPAddress.Parse("10.0.0.1"), PhysicalAddress.Parse("001122334455"));
        RecordingNotifier notifier = new();
        provider.DeviceBuilder.NotifierCallback!(notifier);
        await provider.OnDeviceAddedAsync(client.DeviceId, TestContext.Current.CancellationToken);

        await provider.DeviceBuilder.SubscriptionFeature!.NotifyDeviceRemovedAsync(client.DeviceId, TestContext.Current.CancellationToken);
        int count = notifier.Sent.Count;
        client.Volume = 40;

        Assert.Equal(count, notifier.Sent.Count);
    }

    [Fact]
    public void The_manager_raises_ClientAdded_once_for_a_new_client()
    {
        InMemorySettings settings = new();
        using WebOSClientManager manager = WebOSDeviceProviderTests.CreateManager(settings);
        int raised = 0;
        manager.ClientAdded += (_, _) => raised++;

        manager.GetOrCreateClient(IPAddress.Loopback, PhysicalAddress.Parse("001122334455"));
        manager.GetOrCreateClient(IPAddress.Loopback, PhysicalAddress.Parse("001122334455"));

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task DiscoverAsync_consults_the_brain_endpoint()
    {
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(b => b.ServiceEndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 3000));
        InMemorySettings settings = new();
        using WebOSClientManager manager = new(
            mockBrain.Object,
            NullLogger<WebOSClient>.Instance,
            NullLogger<WebOSClientManager>.Instance,
            settings
        );

        await manager.DiscoverAsync(TestContext.Current.CancellationToken);

        mockBrain.Verify(b => b.ServiceEndPoint, Times.Once);
    }

    private static WebOSClientManager CreateManager(InMemorySettings settings, IBrain? brain = default) => new(
        brain ?? Mock.Of<IBrain>(b => b.ServiceEndPoint == new IPEndPoint(IPAddress.Loopback, 3000)),
        NullLogger<WebOSClient>.Instance,
        NullLogger<WebOSClientManager>.Instance,
        settings
    );

    private sealed class InMemorySettings : Dictionary<string, string>, ISettingsStore<WebOSClientManager>
    {
        public InMemorySettings() : base(StringComparer.Ordinal)
        {
        }

        public string? Get(string key, string? defaultValue = default) => this.TryGetValue(key, out string? value) ? value : defaultValue;

        public void Update(Action<IDictionary<string, string>> action) => action(this);
    }
}

file sealed class RecordingNotifier : IDeviceNotifier
{
    private readonly Lock _lock = new();
    private readonly List<(string Component, object Value, string DeviceId)> _sent = [];
    private readonly List<bool> _powerStates = [];

    public List<(string Component, object Value, string DeviceId)> Sent
    {
        get { lock (this._lock) { return [.. this._sent]; } }
    }

    public List<bool> PowerStates
    {
        get { lock (this._lock) { return [.. this._powerStates]; } }
    }

    public Task NotifyAsync(string componentName, object value, string deviceId = "default", CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task NotifyPowerStateAsync(bool powerState, string deviceId = "default", CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public IDisposable NotifyOnChange<TValue>(string componentName, IObservable<TValue> values, string deviceId = "default")
        where TValue : notnull
    {
        return values.DistinctUntilChanged().Subscribe(value =>
        {
            lock (this._lock)
            {
                this._sent.Add((componentName, value, deviceId));
            }
        });
    }

    public IDisposable NotifyPowerStateOnChange(IObservable<bool> values, string deviceId = "default")
    {
        return values.DistinctUntilChanged().Subscribe(value =>
        {
            lock (this._lock)
            {
                this._powerStates.Add(value);
            }
        });
    }
}
