using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
/// Pair with <see cref="IDeviceBuilder.AddPowerStateSensor{TClient}"/> to derive a device's power state from its
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
        ArgumentNullException.ThrowIfNull(deviceId);
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
    public bool TryAdd(TClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(client.DeviceId, nameof(client));
        Entry entry = new(client);
        entry.Handler = (_, _) => this.OnConnectionChanged(entry);
        // Subscribe before publishing the entry (and outside the lock, so the registry's lock is never held while
        // calling into the client) - a change raised in between is ignored until the entry is registered, and the
        // state read below then picks it up.
        client.ConnectionChanged += entry.Handler;
        bool added;
        using (this._lock.EnterScope())
        {
            if (added = this._entries.TryAdd(client.DeviceId, entry))
            {
                entry.IsRegistered = true;
                entry.IsConnected = client.IsConnected;
                this._pending.Enqueue(new(DeviceRegistryChangeKind.Added, client, entry.IsConnected));
            }
        }
        if (!added)
        {
            client.ConnectionChanged -= entry.Handler;
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
        ArgumentNullException.ThrowIfNull(deviceId);
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
        entry.Client.ConnectionChanged -= entry.Handler;
        this.DispatchPending();
        client = entry.Client;
        return true;
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

        public EventHandler Handler { get; set; } = static delegate { };

        // The following are only read/written under the registry's lock.
        public bool IsConnected { get; set; }

        public bool IsRegistered { get; set; }
    }
}
