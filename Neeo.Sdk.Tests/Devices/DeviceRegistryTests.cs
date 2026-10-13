using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class DeviceRegistryTests
{
    [Fact]
    public void TryAdd_should_register_client_and_raise_Added_with_connection_state()
    {
        DeviceRegistry<FakeClient> registry = new();
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = DeviceRegistryTests.Record(registry);
        FakeClient client = new("a", isConnected: true);

        Assert.True(registry.TryAdd(client));

        Assert.Same(client, registry.GetClient("a"));
        Assert.Equal([client], registry.Clients);
        DeviceRegistryChangedEventArgs<FakeClient> change = Assert.Single(changes);
        Assert.Equal((DeviceRegistryChangeKind.Added, "a", true), (change.Kind, change.DeviceId, change.IsConnected));
        Assert.Same(client, change.Client);
    }

    [Fact]
    public void TryAdd_with_duplicate_device_id_should_fail_without_observing_the_rejected_client()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient first = new("a");
        FakeClient duplicate = new("a");
        registry.TryAdd(first);
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = DeviceRegistryTests.Record(registry);

        Assert.False(registry.TryAdd(duplicate));

        Assert.Same(first, registry.GetClient("a"));
        Assert.Equal(0, duplicate.SubscriberCount);
        duplicate.SetConnected(true);
        Assert.Empty(changes);
    }

    [Fact]
    public void TryAdd_should_throw_for_missing_device_id()
    {
        DeviceRegistry<FakeClient> registry = new();

        Assert.Throws<ArgumentNullException>(() => registry.TryAdd(null!));
        Assert.Throws<ArgumentException>(() => registry.TryAdd(new(string.Empty)));
    }

    [Fact]
    public void Connection_changes_should_raise_Connected_and_Disconnected_only_on_transitions()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient client = new("a");
        registry.TryAdd(client);
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = DeviceRegistryTests.Record(registry);

        client.SetConnected(true);
        client.RaiseConnectionChanged(); // No actual change.
        client.SetConnected(false);

        Assert.Equal(
            [(DeviceRegistryChangeKind.Connected, true), (DeviceRegistryChangeKind.Disconnected, false)],
            changes.Select(static change => (change.Kind, change.IsConnected))
        );
    }

    [Fact]
    public void TryGetConnected_should_reflect_live_client_state()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient client = new("a");
        registry.TryAdd(client);

        Assert.False(registry.TryGetConnected("a", out FakeClient? notConnected));
        Assert.Null(notConnected);

        client.IsConnected = true; // Changed without raising ConnectionChanged.

        Assert.True(registry.TryGetConnected("a", out FakeClient? connected));
        Assert.Same(client, connected);
        Assert.False(registry.TryGetConnected("missing", out _));
        Assert.Null(registry.GetClient("missing"));
    }

    [Fact]
    public void TryRemove_should_raise_Removed_and_stop_observing_the_client()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient client = new("a", isConnected: true);
        registry.TryAdd(client);
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = DeviceRegistryTests.Record(registry);

        Assert.True(registry.TryRemove("a", out FakeClient? removed));

        Assert.Same(client, removed);
        Assert.Null(registry.GetClient("a"));
        Assert.Empty(registry.Clients);
        Assert.Equal(0, client.SubscriberCount);
        DeviceRegistryChangedEventArgs<FakeClient> change = Assert.Single(changes);
        Assert.Equal((DeviceRegistryChangeKind.Removed, false), (change.Kind, change.IsConnected));
        client.SetConnected(false);
        Assert.Single(changes);
        Assert.False(registry.Remove("a"));
        Assert.False(registry.TryRemove("a", out FakeClient? none));
        Assert.Null(none);
    }

    [Fact]
    public void Remove_should_allow_a_new_client_for_the_same_device_id()
    {
        DeviceRegistry<FakeClient> registry = new();
        registry.TryAdd(new("a"));
        FakeClient replacement = new("a", isConnected: true);

        Assert.True(registry.Remove("a"));
        Assert.True(registry.TryAdd(replacement));

        Assert.Same(replacement, registry.GetClient("a"));
    }

    [Fact]
    public void Changes_queued_from_within_a_handler_should_be_raised_after_the_current_change()
    {
        DeviceRegistry<FakeClient> registry = new();
        List<DeviceRegistryChangeKind> kinds = [];
        registry.Changed += (_, e) =>
        {
            kinds.Add(e.Kind);
            if (e.Kind == DeviceRegistryChangeKind.Added)
            {
                registry.Remove(e.DeviceId);
                kinds.Add(DeviceRegistryChangeKind.Added); // Marks that Remove returned before Removed was raised.
            }
        };

        registry.TryAdd(new("a"));

        Assert.Equal([DeviceRegistryChangeKind.Added, DeviceRegistryChangeKind.Added, DeviceRegistryChangeKind.Removed], kinds);
    }

    [Fact]
    public void A_failing_handler_should_not_stop_later_changes()
    {
        DeviceRegistry<FakeClient> registry = new();
        int calls = 0;
        registry.Changed += (_, _) =>
        {
            calls++;
            throw new InvalidOperationException("Boom");
        };
        FakeClient client = new("a");

        registry.TryAdd(client);
        client.SetConnected(true);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Concurrent_changes_should_be_raised_one_at_a_time_and_end_in_the_final_state()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient[] clients = [.. Enumerable.Range(0, 8).Select(static index => new FakeClient($"device-{index}"))];
        ConcurrentDictionary<string, bool> lastReported = new();
        int concurrentHandlers = 0, maxConcurrentHandlers = 0;
        registry.Changed += (_, e) =>
        {
            int current = Interlocked.Increment(ref concurrentHandlers);
            InterlockedMax(ref maxConcurrentHandlers, current);
            lastReported[e.DeviceId] = e.IsConnected;
            Interlocked.Decrement(ref concurrentHandlers);
        };
        Array.ForEach(clients, client => registry.TryAdd(client));

        await Task.WhenAll(clients.Select(static client => Task.Run(() =>
        {
            for (int iteration = 0; iteration < 500; iteration++)
            {
                client.SetConnected(iteration % 2 == 0);
            }
        })));

        Assert.Equal(1, maxConcurrentHandlers);
        Assert.All(clients, client => Assert.Equal(client.IsConnected, lastReported[client.DeviceId]));

        static void InterlockedMax(ref int location, int value)
        {
            int current;
            while ((current = Volatile.Read(ref location)) < value && Interlocked.CompareExchange(ref location, value, current) != current)
            {
            }
        }
    }

    [Fact]
    public async Task AddPowerStateSensor_with_registry_should_derive_power_state_from_the_connected_client()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient client = new("a");
        registry.TryAdd(client);
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio).AddPowerStateSensor(registry);
        IValueFeature feature = Assert.IsAssignableFrom<IValueFeature>(builder.BuildAdapter().GetFeature("powerstate"));

        Assert.True(builder.HasPowerStateSensor);
        Assert.Equal(false, (await feature.GetValueAsync("a", TestContext.Current.CancellationToken)).Value);
        client.IsConnected = true;
        Assert.Equal(true, (await feature.GetValueAsync("a", TestContext.Current.CancellationToken)).Value);
        Assert.Equal(false, (await feature.GetValueAsync("missing", TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task AddPowerStateSensor_with_registry_should_use_a_custom_sensor_when_given()
    {
        DeviceRegistry<FakeClient> registry = new();
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddPowerStateSensor(registry, static (_, _) => Task.FromResult(true));
        IValueFeature feature = Assert.IsAssignableFrom<IValueFeature>(builder.BuildAdapter().GetFeature("powerstate"));

        Assert.Equal(true, (await feature.GetValueAsync("missing", TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public void AddPowerStateSensor_with_registry_should_send_power_notifications_for_registry_changes()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient existing = new("a", isConnected: true);
        registry.TryAdd(existing);
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio).AddPowerStateSensor(registry);
        List<(bool, string)> notifications = [];
        Mock<IDeviceNotifier> notifier = new(MockBehavior.Strict);
        notifier
            .Setup(n => n.SendPowerNotificationAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((bool powerState, string deviceId, CancellationToken _) => notifications.Add((powerState, deviceId)))
            .Returns(Task.CompletedTask);

        Assert.NotNull(builder.NotifierCallback);
        builder.NotifierCallback(notifier.Object);
        FakeClient added = new("b");
        registry.TryAdd(added);
        added.SetConnected(true);
        existing.SetConnected(false);
        registry.Remove("b");

        // Nothing is sent for changes before the server started (the Brain queries the sensor instead).
        Assert.Equal([(false, "b"), (true, "b"), (false, "a"), (false, "b")], notifications);
    }

    [Fact]
    public void AddPowerStateSensor_with_registry_should_coexist_with_EnableNotifications()
    {
        DeviceRegistry<FakeClient> registry = new();
        IDeviceNotifier? received = null;
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio)
            .AddPowerStateSensor(registry)
            .EnableNotifications(notifier => received = notifier);
        Mock<IDeviceNotifier> notifier = new();

        builder.NotifierCallback!(notifier.Object);
        registry.TryAdd(new("a", isConnected: true));

        Assert.Same(notifier.Object, received);
        notifier.Verify(n => n.SendPowerNotificationAsync(true, "a", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Throws<InvalidOperationException>(() => builder.EnableNotifications(static _ => { }));
    }

    [Fact]
    public void AddPowerStateSensor_with_registry_should_throw_for_null_registry_or_existing_sensor()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio);

        Assert.Throws<ArgumentNullException>(() => builder.AddPowerStateSensor<FakeClient>(null!));
        builder.AddPowerStateSensor(new DeviceRegistry<FakeClient>());
        Assert.Throws<InvalidOperationException>(() => builder.AddPowerStateSensor(new DeviceRegistry<FakeClient>()));
    }

    private static List<DeviceRegistryChangedEventArgs<FakeClient>> Record(IDeviceRegistry<FakeClient> registry)
    {
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = [];
        registry.Changed += (_, e) => changes.Add(e);
        return changes;
    }

    private sealed class FakeClient(string deviceId, bool isConnected = false) : IDeviceClient
    {
        private EventHandler? _connectionChanged;
        private int _subscriberCount;
        private volatile bool _isConnected = isConnected;

        public event EventHandler? ConnectionChanged
        {
            add
            {
                this._connectionChanged += value;
                Interlocked.Increment(ref this._subscriberCount);
            }
            remove
            {
                this._connectionChanged -= value;
                Interlocked.Decrement(ref this._subscriberCount);
            }
        }

        public string DeviceId { get; } = deviceId;

        public bool IsConnected
        {
            get => this._isConnected;
            set => this._isConnected = value;
        }

        public int SubscriberCount => Volatile.Read(ref this._subscriberCount);

        public void RaiseConnectionChanged() => this._connectionChanged?.Invoke(this, EventArgs.Empty);

        public void SetConnected(bool isConnected)
        {
            this.IsConnected = isConnected;
            this.RaiseConnectionChanged();
        }
    }
}
