using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Devices;

/// <summary>
/// The kind of change reported by <see cref="IDeviceRegistry{TClient}.Changed"/>.
/// </summary>
public enum DeviceRegistryChangeKind
{
    /// <summary>
    /// A client was added to the registry.
    /// </summary>
    Added,

    /// <summary>
    /// A client was removed from the registry.
    /// </summary>
    Removed,

    /// <summary>
    /// A registered client connected to its device.
    /// </summary>
    Connected,

    /// <summary>
    /// A registered client disconnected from its device.
    /// </summary>
    Disconnected,
}

/// <summary>
/// Event arguments describing a change to an <see cref="IDeviceRegistry{TClient}"/>.
/// </summary>
/// <typeparam name="TClient">The type of the client.</typeparam>
/// <param name="kind">The kind of change.</param>
/// <param name="client">The client that changed.</param>
/// <param name="isConnected">Whether the client is connected as of this change (always <see langword="false"/> for <see cref="DeviceRegistryChangeKind.Removed"/>).</param>
public sealed class DeviceRegistryChangedEventArgs<TClient>(DeviceRegistryChangeKind kind, TClient client, bool isConnected) : EventArgs
    where TClient : class, IDeviceClient
{
    /// <summary>
    /// Gets the kind of change.
    /// </summary>
    public DeviceRegistryChangeKind Kind { get; } = kind;

    /// <summary>
    /// Gets the client that changed.
    /// </summary>
    public TClient Client { get; } = client;

    /// <summary>
    /// Gets the identifier of the device whose client changed.
    /// </summary>
    public string DeviceId { get; } = client.DeviceId;

    /// <summary>
    /// Gets a value indicating whether the client is connected as of this change
    /// (always <see langword="false"/> for <see cref="DeviceRegistryChangeKind.Removed"/>).
    /// </summary>
    public bool IsConnected { get; } = isConnected;
}

/// <summary>
/// A thread-safe registry of a driver's device clients, keyed by <see cref="IDeviceClient.DeviceId"/>, which tracks each
/// client's connection state and reports changes via <see cref="Changed"/>.
/// <para/>
/// Pair with <see cref="IDeviceBuilder.AddPowerStateSensor{TClient}(IDeviceRegistry{TClient}, AsyncDeviceValueGetter{bool})"/> to derive a device's power state from its
/// client's connection, with power notifications sent to the NEEO Brain automatically.
/// </summary>
/// <typeparam name="TClient">The type of the client.</typeparam>
public interface IDeviceRegistry<TClient>
    where TClient : class, IDeviceClient
{
    /// <summary>
    /// Raised (in order, one at a time) when a client is added, removed, connects or disconnects.
    /// Handlers are invoked outside of the registry's lock, so they may safely call back into the registry.
    /// </summary>
    event EventHandler<DeviceRegistryChangedEventArgs<TClient>>? Changed;

    /// <summary>
    /// Gets a snapshot of the registered clients.
    /// </summary>
    IReadOnlyCollection<TClient> Clients { get; }

    /// <summary>
    /// Gets the client for the specified <paramref name="deviceId"/> (regardless of its connection state), or <see langword="null"/> if not registered.
    /// </summary>
    /// <param name="deviceId">The identifier of the device.</param>
    /// <returns>The client, or <see langword="null"/>.</returns>
    TClient? GetClient(string deviceId);

    /// <summary>
    /// Gets the client for the specified <paramref name="deviceId"/> if it is registered and currently connected.
    /// </summary>
    /// <param name="deviceId">The identifier of the device.</param>
    /// <param name="client">The connected client, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the client is registered and connected; otherwise <see langword="false"/>.</returns>
    bool TryGetConnected(string deviceId, [NotNullWhen(true)] out TClient? client);

    /// <summary>
    /// Adds a client to the registry, unless a client with the same <see cref="IDeviceClient.DeviceId"/> is already registered.
    /// </summary>
    /// <param name="client">The client to add.</param>
    /// <returns><see langword="true"/> if the client was added; otherwise <see langword="false"/>.</returns>
    bool TryAdd(TClient client);

    /// <summary>
    /// Gets the client for the specified <paramref name="deviceId"/>, adding the one created by <paramref name="factory"/> if
    /// none is registered. The factory runs under the registry's lock, so it must be cheap (construct the client - don't connect
    /// it) and must not call back into the registry. It is only invoked when no client is registered, and the client it returns
    /// must have the same <see cref="IDeviceClient.DeviceId"/>.
    /// </summary>
    /// <param name="deviceId">The identifier of the device.</param>
    /// <param name="factory">Creates the client to add if none is registered.</param>
    /// <param name="added"><see langword="true"/> if the client was created and added by this call; otherwise <see langword="false"/>.</param>
    /// <returns>The registered client.</returns>
    TClient GetOrAdd(string deviceId, Func<string, TClient> factory, out bool added);

    /// <summary>
    /// Removes the client for the specified <paramref name="deviceId"/>. The registry stops observing the client but does not dispose it.
    /// </summary>
    /// <param name="deviceId">The identifier of the device.</param>
    /// <param name="client">The removed client, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a client was removed; otherwise <see langword="false"/>.</returns>
    bool TryRemove(string deviceId, [NotNullWhen(true)] out TClient? client);

    /// <summary>
    /// Removes the client for the specified <paramref name="deviceId"/>. The registry stops observing the client but does not dispose it.
    /// </summary>
    /// <param name="deviceId">The identifier of the device.</param>
    /// <returns><see langword="true"/> if a client was removed; otherwise <see langword="false"/>.</returns>
    bool Remove(string deviceId);

    /// <summary>
    /// Invokes <paramref name="attach"/> once for each currently registered client and each client subsequently added,
    /// and disposes the returned subscription when that client is removed from the registry or when the returned
    /// <see cref="IDisposable"/> is disposed. <paramref name="attach"/> and subscription disposal always run outside the
    /// registry's lock; because <paramref name="attach"/> runs on the serialized <see cref="Changed"/> dispatch path for
    /// newly added clients, it should be non-blocking.
    /// </summary>
    /// <param name="attach">Attaches to a registered client and returns a subscription to dispose when the client is removed.</param>
    /// <returns>A subscription that detaches from all clients and stops observing the registry when disposed.</returns>
    IDisposable Attach(Func<TClient, IDisposable> attach);

    /// <summary>
    /// Subscribes to <paramref name="selector"/> for each currently registered client and each client subsequently added,
    /// unsubscribing when that client is removed from the registry, and emits each <c>(Client, Value)</c> pair (serialized
    /// across clients). If a client's stream replays its current value upon subscription (such as a <c>BehaviorSubject</c>),
    /// that replay is emitted when the client is attached unless filtered within <paramref name="selector"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of value emitted by the per-client observable.</typeparam>
    /// <param name="selector">Selects the observable stream to observe for a registered client.</param>
    /// <returns>An observable sequence of <c>(Client, Value)</c> pairs across all registered clients.</returns>
    IObservable<(TClient Client, TValue Value)> Observe<TValue>(Func<TClient, IObservable<TValue>> selector);
}

/// <summary>
/// Default thread-safe implementation of <see cref="IDeviceRegistry{TClient}"/>.
/// </summary>
/// <typeparam name="TClient">The type of the client.</typeparam>
/// <param name="logger">Optional logger used to report exceptions thrown by <see cref="Changed"/> handlers.</param>
public sealed class DeviceRegistry<TClient>(ILogger? logger = null) : IDeviceRegistry<TClient>
    where TClient : class, IDeviceClient
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();
    private readonly Queue<DeviceRegistryChangedEventArgs<TClient>> _pending = new();
    private bool _isDispatching;

    /// <inheritdoc/>
    public event EventHandler<DeviceRegistryChangedEventArgs<TClient>>? Changed;

    /// <inheritdoc/>
    public IReadOnlyCollection<TClient> Clients
    {
        get
        {
            using (this._lock.EnterScope())
            {
                return [.. this._entries.Values.Select(static entry => entry.Client)];
            }
        }
    }

    /// <inheritdoc/>
    public TClient? GetClient(string deviceId)
    {
        using (this._lock.EnterScope())
        {
            return this._entries.GetValueOrDefault(deviceId)?.Client;
        }
    }

    /// <inheritdoc/>
    public bool TryGetConnected(string deviceId, [NotNullWhen(true)] out TClient? client)
    {
        // Reads the client's live state rather than the last reported one, so a caller never acts on a stale "connected".
        if (this.GetClient(deviceId) is { IsConnected: true } connected)
        {
            client = connected;
            return true;
        }
        client = null;
        return false;
    }

    /// <inheritdoc/>
    public TClient GetOrAdd(string deviceId, Func<string, TClient> factory, out bool added)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceId);
        Entry entry;
        using (this._lock.EnterScope())
        {
            if (this._entries.GetValueOrDefault(deviceId) is { } existing)
            {
                added = false;
                return existing.Client;
            }
            TClient client = factory(deviceId);
            if (!string.Equals(client.DeviceId, deviceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The factory created a client for device '{client.DeviceId}' instead of '{deviceId}'.");
            }
            entry = new(client);
            // Subscribing under the lock is safe here: the client was just created, so nothing else can be emitting on it yet
            // (and an initial synchronous replay is ignored because IsRegistered is still false).
            entry.Subscription = client.IsConnectedChanged.Subscribe(_ => this.OnConnectionChanged(entry));
            this.Register(deviceId, entry);
        }
        this.DispatchPending();
        added = true;
        return entry.Client;
    }

    /// <inheritdoc/>
    public bool TryAdd(TClient client)
    {
        ArgumentException.ThrowIfNullOrEmpty(client.DeviceId, nameof(client));
        Entry entry = new(client);
        // Subscribe before publishing the entry (and outside the lock, so the registry's lock is never held while
        // calling into the client) - a change emitted in between (or an initial replay) is ignored until the entry is
        // registered, and the state read below then picks it up.
        IDisposable subscription = client.IsConnectedChanged.Subscribe(_ => this.OnConnectionChanged(entry));
        entry.Subscription = subscription;
        bool added;
        using (this._lock.EnterScope())
        {
            if (added = !this._entries.ContainsKey(client.DeviceId))
            {
                this.Register(client.DeviceId, entry);
            }
        }
        if (!added)
        {
            subscription.Dispose();
            return false;
        }
        this.DispatchPending();
        return true;
    }

    /// <inheritdoc/>
    public bool Remove(string deviceId) => this.TryRemove(deviceId, out _);

    /// <inheritdoc/>
    public bool TryRemove(string deviceId, [NotNullWhen(true)] out TClient? client)
    {
        Entry? entry;
        using (this._lock.EnterScope())
        {
            if (!this._entries.Remove(deviceId, out entry))
            {
                client = null;
                return false;
            }
            entry.IsRegistered = false;
            this._pending.Enqueue(new(DeviceRegistryChangeKind.Removed, entry.Client, false));
        }
        entry.Subscription.Dispose();
        this.DispatchPending();
        client = entry.Client;
        return true;
    }

    /// <inheritdoc/>
    public IDisposable Attach(Func<TClient, IDisposable> attach)
    {
        Lock subscriptionLock = new();
        Dictionary<TClient, IDisposable> subscriptions = new(ReferenceEqualityComparer.Instance);
        bool isDisposed = false;
        this.Changed += OnRegistryChanged;
        foreach (TClient client in this.Clients)
        {
            AttachClient(client);
        }
        return Disposable.Create(() =>
        {
            this.Changed -= OnRegistryChanged;
            IDisposable[] toDispose;
            using (subscriptionLock.EnterScope())
            {
                if (isDisposed)
                {
                    return;
                }
                isDisposed = true;
                toDispose = [.. subscriptions.Values];
                subscriptions.Clear();
            }
            Array.ForEach(toDispose, static subscription => subscription.Dispose());
        });

        void OnRegistryChanged(object? sender, DeviceRegistryChangedEventArgs<TClient> e)
        {
            switch (e.Kind)
            {
                case DeviceRegistryChangeKind.Added:
                    AttachClient(e.Client);
                    break;
                case DeviceRegistryChangeKind.Removed:
                    DetachClient(e.Client);
                    break;
            }
        }

        void AttachClient(TClient client)
        {
            using (subscriptionLock.EnterScope())
            {
                if (isDisposed || subscriptions.ContainsKey(client) || !object.ReferenceEquals(this.GetClient(client.DeviceId), client))
                {
                    return;
                }
                subscriptions.Add(client, Disposable.Empty);
            }
            IDisposable subscription;
            try
            {
                subscription = attach(client);
            }
            catch (Exception e)
            {
                using (subscriptionLock.EnterScope())
                {
                    subscriptions.Remove(client);
                }
                logger?.LogError(e, "Failed to attach to device {deviceId}.", client.DeviceId);
                return;
            }
            bool keep;
            using (subscriptionLock.EnterScope())
            {
                keep = !isDisposed && subscriptions.ContainsKey(client) && object.ReferenceEquals(this.GetClient(client.DeviceId), client);
                if (keep)
                {
                    subscriptions[client] = subscription;
                }
                else
                {
                    subscriptions.Remove(client);
                }
            }
            if (!keep)
            {
                subscription.Dispose();
            }
        }

        void DetachClient(TClient client)
        {
            IDisposable? subscription;
            using (subscriptionLock.EnterScope())
            {
                if (!subscriptions.Remove(client, out subscription))
                {
                    return;
                }
            }
            subscription.Dispose();
        }
    }

    /// <inheritdoc/>
    public IObservable<(TClient Client, TValue Value)> Observe<TValue>(Func<TClient, IObservable<TValue>> selector)
    {
        return Observable.Create<(TClient Client, TValue Value)>(observer =>
        {
            IObserver<(TClient Client, TValue Value)> synchronized = Observer.Synchronize(observer);
            return this.Attach(client => selector(client).Subscribe(
                value => synchronized.OnNext((client, value)),
                e => logger?.LogError(e, "An observed stream faulted for {deviceId}.", client.DeviceId)
            ));
        });
    }

    // Must be called under the lock, with no entry registered for deviceId.
    private void Register(string deviceId, Entry entry)
    {
        this._entries.Add(deviceId, entry);
        entry.IsRegistered = true;
        entry.IsConnected = entry.Client.IsConnected;
        this._pending.Enqueue(new(DeviceRegistryChangeKind.Added, entry.Client, entry.IsConnected));
    }

    private void OnConnectionChanged(Entry entry)
    {
        using (this._lock.EnterScope())
        {
            if (!entry.IsRegistered)
            {
                return;
            }
            bool isConnected = entry.Client.IsConnected;
            if (isConnected == entry.IsConnected)
            {
                return;
            }
            entry.IsConnected = isConnected;
            this._pending.Enqueue(new(isConnected ? DeviceRegistryChangeKind.Connected : DeviceRegistryChangeKind.Disconnected, entry.Client, isConnected));
        }
        this.DispatchPending();
    }

    /// <summary>
    /// Raises queued changes one at a time, in the order they occurred. Only one thread dispatches at a time; a change
    /// queued while another thread is dispatching (including from within a handler) is raised by that thread.
    /// </summary>
    private void DispatchPending()
    {
        using (this._lock.EnterScope())
        {
            if (this._isDispatching)
            {
                return;
            }
            this._isDispatching = true;
        }
        while (true)
        {
            DeviceRegistryChangedEventArgs<TClient>? args;
            using (this._lock.EnterScope())
            {
                if (!this._pending.TryDequeue(out args))
                {
                    this._isDispatching = false;
                    return;
                }
            }
            try
            {
                this.Changed?.Invoke(this, args);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "A device registry change handler failed for {deviceId} ({kind}).", args.DeviceId, args.Kind);
            }
        }
    }

    private sealed class Entry(TClient client)
    {
        public TClient Client { get; } = client;

        public IDisposable Subscription { get; set; } = Disposable.Empty;

        // The following are only read/written under the registry's lock.
        public bool IsConnected { get; set; }

        public bool IsRegistered { get; set; }
    }
}
