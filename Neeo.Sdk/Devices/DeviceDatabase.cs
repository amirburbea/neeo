﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities.TokenSearch;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Interface for a device database.
/// </summary>
public interface IDeviceDatabase
{
    /// <summary>
    /// Gets the collection of device adapters.
    /// </summary>
    /// <remarks>Note that the adapters may not have run their associated initializer.</remarks>
    IEnumerable<IDeviceAdapter> Adapters { get; }

    /// <summary>
    /// Get the adapter with the specified <paramref name="adapterName"/>. If the adapter
    /// has a registered initializer, ensures the adapter is initialized.
    /// </summary>
    /// <param name="adapterName">The name of the adapter.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests (defaults to <see cref="CancellationToken.None"/>).</param>
    /// <returns><see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the associated model for an adapter with the specified <paramref name="adapterName"/>, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="adapterName">The name of the device adapter.</param>
    /// <returns>The device model if it exists, <see langword="null"/> otherwise.</returns>
    DeviceModel? GetDeviceByAdapterName(string adapterName);

    /// <summary>
    /// Gets the device adapter model with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The device identifier.</param>
    /// <returns>The device model if it exists, <see langword="null"/> otherwise.</returns>
    DeviceModel? GetDeviceById(int id);

    /// <summary>
    /// Searches for a device with a token matching the search <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>An array of sort entries ranked as per a similar algorithm to &quot;tokenseach.js&quot;.</returns>
    SearchEntry<DeviceModel>[] Search(string? query);
}

internal sealed class DeviceDatabase : IDeviceDatabase
{
    private readonly Dictionary<string, DeviceAdapterContainer> _containers;
    private readonly TokenSearch<DeviceModel> _deviceIndex;
    private readonly DeviceModel[] _devices;
    private readonly INotificationService _notificationService;

    public DeviceDatabase(IReadOnlyCollection<IDeviceBuilder> devices, INotificationService notificationService, ILogger<DeviceDatabase> logger)
    {
        this._notificationService = notificationService;
        this._devices = new DeviceModel[devices.Count];
        this._containers = new(this._devices.Length);
        foreach (IDeviceBuilder device in devices)
        {
            IDeviceAdapter adapter = device.BuildAdapter();
            int id = this._containers.Count;
            this._devices[id] = new(id, adapter);
            if (!this._containers.TryAdd(adapter.AdapterName, new(adapter, logger)))
            {
                throw new ArgumentException($"Adapter names must be unique. Adapter name {adapter.AdapterName} is reused at index {id}");
            }
            if (device.NotifierCallback is { } callback)
            {
                callback(new DeviceNotifier(adapter, this._notificationService, device.HasPowerStateSensor));
            }
        }
        this._deviceIndex = new(
            this._devices,
            nameof(DeviceModel.Manufacturer),
            nameof(DeviceModel.Name),
            nameof(DeviceModel.Tokens),
            nameof(DeviceModel.Type)
        );
    }

    public IEnumerable<IDeviceAdapter> Adapters => this._containers.Values.Select(static container => container.Adapter);

    public async ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName, CancellationToken cancellationToken = default)
    {
        if (this._containers.TryGetValue(adapterName ?? throw new ArgumentNullException(nameof(adapterName)), out DeviceAdapterContainer? container))
        {
            await container.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        return container?.Adapter;
    }

    public DeviceModel? GetDeviceByAdapterName(string name) => Array.Find(this._devices, device => device.AdapterName == name);

    public DeviceModel? GetDeviceById(int id)
    {
        return id is > -1 && id < this._devices.Length ? this._devices[id] : null;
    }

    public SearchEntry<DeviceModel>[] Search(string? query) => string.IsNullOrEmpty(query)
        ? []
        : this._deviceIndex.Search(query).Take(OptionConstants.MaxSearchResults).ToArray();

    private static class OptionConstants
    {
        public const int MaxSearchResults = 10;
    }

    private sealed class DeviceAdapterContainer(IDeviceAdapter adapter, ILogger logger)
    {
        private Task? _task;

        public IDeviceAdapter Adapter => adapter;

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (adapter.Initializer == null)
            {
                return Task.CompletedTask;
            }
            return this._task is { IsFaulted: false, IsCanceled: false } task
                ? task // Return the currently executing task.
                : InitializeAsync(adapter.Initializer);

            async Task InitializeAsync(DeviceInitializer initializer)
            {
                logger.LogInformation("Initializing adapter {deviceName} ({adapterName})...", adapter.DeviceName, adapter.AdapterName);
                try
                {
                    await (this._task = initializer(cancellationToken)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error initializing adapter.");
                }
            }
        }
    }
}
