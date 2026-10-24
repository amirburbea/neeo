using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public void GetOrAdd_should_create_and_register_a_client_only_when_none_is_registered()
    {
        DeviceRegistry<FakeClient> registry = new();
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = DeviceRegistryTests.Record(registry);
        int created = 0;

        FakeClient first = registry.GetOrAdd("a", id => { created++; return new(id, isConnected: true); }, out bool firstAdded);
        FakeClient second = registry.GetOrAdd("a", id => { created++; return new(id); }, out bool secondAdded);

        Assert.True(firstAdded);
        Assert.False(secondAdded);
        Assert.Same(first, second);
        Assert.Equal(1, created);
        Assert.Equal(1, first.SubscriberCount);
        DeviceRegistryChangedEventArgs<FakeClient> change = Assert.Single(changes);
        Assert.Equal((DeviceRegistryChangeKind.Added, "a", true), (change.Kind, change.DeviceId, change.IsConnected));
        first.SetConnected(false);
        Assert.Equal(DeviceRegistryChangeKind.Disconnected, changes[^1].Kind);
    }

    [Fact]
    public void GetOrAdd_should_reject_a_client_for_a_different_device()
    {
        DeviceRegistry<FakeClient> registry = new();

        Assert.Throws<InvalidOperationException>(() => registry.GetOrAdd("a", static _ => new("b"), out bool _));
        Assert.Throws<ArgumentException>(() => registry.GetOrAdd(string.Empty, static id => new(id), out bool _));
        Assert.Empty(registry.Clients);
    }

    [Fact]
    public async Task GetOrAdd_called_concurrently_should_create_one_client()
    {
        DeviceRegistry<FakeClient> registry = new();
        int created = 0;

        FakeClient[] results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(
            () => registry.GetOrAdd("a", id => { Interlocked.Increment(ref created); return new(id); }, out bool _),
            TestContext.Current.CancellationToken
        )));

        Assert.Equal(1, created);
        Assert.All(results, result => Assert.Same(results[0], result));
    }

    [Fact]
    public void TryAdd_should_throw_for_missing_device_id()
    {
        DeviceRegistry<FakeClient> registry = new();

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
    public void AddPowerStateSensor_with_registry_should_throw_for_existing_sensor()
    {
        IDeviceBuilder builder = Device.Create("Stereo", DeviceType.Audio);

        builder.AddPowerStateSensor(new DeviceRegistry<FakeClient>());
        Assert.Throws<InvalidOperationException>(() => builder.AddPowerStateSensor(new DeviceRegistry<FakeClient>()));
    }

    [Fact]
    public void Attach_should_attach_existing_and_added_clients_and_dispose_on_remove_or_outer_dispose()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient first = new("a");
        FakeClient second = new("b");
        registry.TryAdd(first);
        List<string> attached = [];
        List<string> disposed = [];
        IDisposable subscription = registry.Attach(client =>
        {
            attached.Add(client.DeviceId);
            return Disposable.Create(() => disposed.Add(client.DeviceId));
        });
        Assert.Equal(["a"], attached);
        Assert.Empty(disposed);
        registry.TryAdd(second);
        Assert.Equal(["a", "b"], attached);
        registry.Remove("a");
        Assert.Equal(["a"], disposed);
        subscription.Dispose();
        Assert.Equal(["a", "b"], disposed);
        registry.TryAdd(new("c"));
        Assert.Equal(["a", "b"], attached);
    }

    [Fact]
    public void Observe_should_emit_values_while_registered_and_stop_on_remove()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient first = new("a");
        FakeClient second = new("b");
        registry.TryAdd(first);
        List<(string DeviceId, bool Value)> received = [];
        using IDisposable subscription = registry
            .Observe(static client => client.IsConnectedChanged.DistinctUntilChanged().Skip(1))
            .Subscribe(pair => received.Add((pair.Client.DeviceId, pair.Value)));
        Assert.Empty(received);
        first.SetConnected(true);
        registry.TryAdd(second);
        second.SetConnected(true);
        registry.Remove("a");
        first.SetConnected(false);
        second.SetConnected(false);
        Assert.Equal([("a", true), ("b", true), ("b", false)], received);
    }

    [Fact]
    public void Attach_should_dispose_subscription_when_client_is_removed_during_attach()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient client = new("a");
        registry.TryAdd(client);
        bool disposed = false;
        using IDisposable subscription = registry.Attach(c =>
        {
            registry.Remove(c.DeviceId);
            return Disposable.Create(() => disposed = true);
        });
        Assert.True(disposed);
    }

    [Fact]
    public void Attach_should_not_invoke_callback_when_client_was_already_removed_before_Added_ran()
    {
        DeviceRegistry<FakeClient> registry = new();
        List<string> attached = [];
        registry.Changed += (_, e) =>
        {
            if (e.Kind == DeviceRegistryChangeKind.Added)
            {
                registry.Remove(e.DeviceId);
            }
        };
        using IDisposable subscription = registry.Attach(client =>
        {
            attached.Add(client.DeviceId);
            return Disposable.Empty;
        });
        registry.TryAdd(new("a"));
        Assert.Empty(attached);
    }

    [Fact]
    public void Attach_should_recover_when_callback_throws_and_allow_reattaching()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient failing = new("a");
        FakeClient healthy = new("b");
        registry.TryAdd(failing);
        registry.TryAdd(healthy);
        bool shouldThrow = true;
        List<string> attached = [];
        using IDisposable subscription = registry.Attach(client =>
        {
            if (client.DeviceId == "a" && shouldThrow)
            {
                throw new InvalidOperationException("Boom");
            }
            attached.Add(client.DeviceId);
            return Disposable.Empty;
        });
        Assert.Equal(["b"], attached);
        shouldThrow = false;
        registry.Remove("a");
        registry.TryAdd(failing);
        Assert.Equal(["b", "a"], attached);
    }

    [Fact]
    public void Observe_should_keep_other_clients_alive_when_one_client_stream_faults()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient faulting = new("a");
        FakeClient healthy = new("b");
        registry.TryAdd(faulting);
        registry.TryAdd(healthy);
        Subject<int> faultingStream = new();
        Subject<int> healthyStream = new();
        List<(string DeviceId, int Value)> received = [];
        bool observerFaulted = false;
        using IDisposable subscription = registry
            .Observe(client => client.DeviceId == "a" ? faultingStream : healthyStream)
            .Subscribe(pair => received.Add((pair.Client.DeviceId, pair.Value)), _ => observerFaulted = true);
        faultingStream.OnNext(1);
        faultingStream.OnError(new InvalidOperationException("Stream fault"));
        healthyStream.OnNext(2);
        Assert.False(observerFaulted);
        Assert.Equal([("a", 1), ("b", 2)], received);
    }

    [Fact]
    public async Task Observe_should_serialize_concurrent_emissions_from_multiple_clients()
    {
        DeviceRegistry<FakeClient> registry = new();
        FakeClient[] clients = [.. Enumerable.Range(0, 8).Select(static i => new FakeClient($"device-{i}"))];
        Array.ForEach(clients, client => registry.TryAdd(client));
        int inFlight = 0, maxInFlight = 0, count = 0;
        using IDisposable subscription = registry
            .Observe(static client => client.IsConnectedChanged)
            .Subscribe(_ =>
            {
                int current = Interlocked.Increment(ref inFlight);
                int snapshot;
                while ((snapshot = Volatile.Read(ref maxInFlight)) < current && Interlocked.CompareExchange(ref maxInFlight, current, snapshot) != snapshot)
                {
                }
                Interlocked.Increment(ref count);
                // Linger so that unserialized emissions from other threads would overlap this one.
                Thread.Yield();
                Interlocked.Decrement(ref inFlight);
            });
        await Task.WhenAll(clients.Select(static client => Task.Run(() =>
        {
            for (int iteration = 0; iteration < 500; iteration++)
            {
                client.RaiseConnectionChanged();
            }
        })));
        Assert.Equal(1, maxInFlight);
        Assert.Equal(8 + (8 * 500), count);
    }

    [Fact]
    public async Task Attach_racing_with_concurrent_add_remove_and_dispose_should_not_leak_subscriptions()
    {
        DeviceRegistry<FakeClient> registry = new();
        int activeSubscriptions = 0;
        IDisposable[] observers = new IDisposable[4];
        for (int i = 0; i < observers.Length; i++)
        {
            observers[i] = registry.Attach(_ =>
            {
                Interlocked.Increment(ref activeSubscriptions);
                return Disposable.Create(() => Interlocked.Decrement(ref activeSubscriptions));
            });
        }
        await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            string id = $"device-{index % 4}";
            for (int iteration = 0; iteration < 200; iteration++)
            {
                if (iteration % 2 == 0)
                {
                    registry.TryAdd(new(id));
                }
                else
                {
                    registry.Remove(id);
                }
            }
        })));
        Array.ForEach(observers, static observer => observer.Dispose());
        Assert.Equal(0, Volatile.Read(ref activeSubscriptions));
    }

    private static List<DeviceRegistryChangedEventArgs<FakeClient>> Record(IDeviceRegistry<FakeClient> registry)
    {
        List<DeviceRegistryChangedEventArgs<FakeClient>> changes = [];
        registry.Changed += (_, e) => changes.Add(e);
        return changes;
    }

    private sealed class FakeClient : IDeviceClient
    {
        private readonly BehaviorSubject<bool> _isConnectedSubject;
        private int _subscriberCount;
        private volatile bool _isConnected;

        public FakeClient(string deviceId, bool isConnected = false)
        {
            this.DeviceId = deviceId;
            this._isConnected = isConnected;
            this._isConnectedSubject = new(isConnected);
            this.IsConnectedChanged = Observable.Create<bool>(observer =>
            {
                Interlocked.Increment(ref this._subscriberCount);
                IDisposable subscription = this._isConnectedSubject.Subscribe(observer);
                return Disposable.Create(() =>
                {
                    subscription.Dispose();
                    Interlocked.Decrement(ref this._subscriberCount);
                });
            });
        }

        public string DeviceId { get; }

        public bool IsConnected
        {
            get => this._isConnected;
            set => this._isConnected = value;
        }

        public IObservable<bool> IsConnectedChanged { get; }

        public int SubscriberCount => Volatile.Read(ref this._subscriberCount);

        public void RaiseConnectionChanged() => this._isConnectedSubject.OnNext(this.IsConnected);

        public void SetConnected(bool isConnected)
        {
            this.IsConnected = isConnected;
            this.RaiseConnectionChanged();
        }
    }
}
