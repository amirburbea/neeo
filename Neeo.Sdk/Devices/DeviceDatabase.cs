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
    IReadOnlyCollection<IDeviceAdapter> Adapters { get; }

    /// <summary>
    /// Get the adapter with the specified <paramref name="adapterName"/>. If the adapter
    /// has a registered initializer, ensures the adapter is initialized.
    /// </summary>
    /// <param name="adapterName">The name of the adapter.</param>
    /// <returns><see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName);

    /// <summary>
    /// Gets the associated model for an adapter with the specified <paramref name="adapterName"/>, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="adapterName">The name of the device adapter.</param>
    /// <returns>The device model if it exists, <see langword="null"/> otherwise.</returns>
    DeviceAdapterModel? GetDeviceByAdapterName(string adapterName);

    /// <summary>
    /// Gets the device adapter model with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The device identifier.</param>
    /// <returns>The device model if it exists, <see langword="null"/> otherwise.</returns>
    DeviceAdapterModel? GetDeviceById(int id);

    /// <summary>
    /// Searches for a device with a token matching the search <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>An array of sort entries ranked as per a similar algorithm to &quot;tokenseach.js&quot;.</returns>
    SearchEntry<DeviceAdapterModel>[] Search(string? query);
}

internal sealed class DeviceDatabase : IDeviceDatabase
{
    private readonly Dictionary<string, IDeviceAdapter> _adapters;
    private readonly TokenSearch<DeviceAdapterModel> _deviceIndex;
    private readonly DeviceAdapterModel[] _devices;
    private readonly Dictionary<string, Task> _initializationTasks = new();
    private readonly HashSet<string> _initializedAdapters = new();
    private readonly ILogger<IDeviceDatabase> _logger;
    private readonly INotificationService _notificationService;
    private readonly ReaderWriterLockSlim _readerWriterLockSlim = new();

    public DeviceDatabase(IReadOnlyCollection<IDeviceBuilder> devices, INotificationService notificationService, ILogger<IDeviceDatabase> logger)
    {
        this._notificationService = notificationService;
        this._logger = logger;
        this._devices = new DeviceAdapterModel[devices.Count];
        this._adapters = new(this._devices.Length);
        foreach (IDeviceBuilder device in devices)
        {
            IDeviceAdapter adapter = device.BuildAdapter();
            int id = this._adapters.Count;
            this._devices[id] = new(id, adapter);
            this._adapters.Add(adapter.AdapterName, adapter);
            if (device.NotifierCallback is { } callback)
            {
                callback(new DeviceNotifier(adapter, this._notificationService, device.HasPowerStateSensor));
            }
        }
        this._deviceIndex = new(
            this._devices,
            nameof(DeviceAdapterModel.Manufacturer),
            nameof(DeviceAdapterModel.Name),
            nameof(DeviceAdapterModel.Tokens),
            nameof(DeviceAdapterModel.Type)
        );
    }

    IReadOnlyCollection<IDeviceAdapter> IDeviceDatabase.Adapters => this._adapters.Values;

    public async ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName)
    {
        if (this._adapters.TryGetValue(adapterName ?? throw new ArgumentNullException(nameof(adapterName)), out IDeviceAdapter? adapter) && adapter.Initializer is { } initializer)
        {
            await this.InitializeDeviceAsync(adapter.AdapterName, initializer).ConfigureAwait(false);
        }
        return adapter;
    }

    public DeviceAdapterModel? GetDeviceByAdapterName(string name)
    {
        return Array.FindIndex(this._devices, device => device.AdapterName == name) is int index and > -1 ? this._devices[index] : null;
    }

    public DeviceAdapterModel? GetDeviceById(int id)
    {
        return id >= 0 && id < this._devices.Length ? this._devices[id] : null;
    }

    public SearchEntry<DeviceAdapterModel>[] Search(string? query) => string.IsNullOrEmpty(query)
        ? Array.Empty<SearchEntry<DeviceAdapterModel>>()
        : this._deviceIndex.Search(query).Take(OptionConstants.MaxSearchResults).ToArray();

    private async ValueTask InitializeDeviceAsync(string adapterName, DeviceInitializer initializer)
    {
        Task? inProgressTask;
        try
        {
            this._readerWriterLockSlim.EnterReadLock();
            if (this._initializedAdapters.Contains(adapterName))
            {
                return;
            }
            inProgressTask = this._initializationTasks.GetValueOrDefault(adapterName);
        }
        finally
        {
            this._readerWriterLockSlim.ExitReadLock();
        }
        if (inProgressTask != null)
        {
            await inProgressTask.ConfigureAwait(false);
            return;
        }
        bool started = false;
        try
        {
            this._readerWriterLockSlim.EnterWriteLock();
            if (!this._initializationTasks.TryGetValue(adapterName, out inProgressTask))
            {
                this._logger.LogInformation("Initializing device: {name}", adapterName);
                this._initializationTasks.TryAdd(adapterName, inProgressTask = initializer());
                started = true;
            }
        }
        finally
        {
            this._readerWriterLockSlim.ExitWriteLock();
        }
        bool success;
        try
        {
            await inProgressTask.ConfigureAwait(false);
            success = true;
        }
        catch (Exception e)
        {
            if (started) // If this is not the execution chain which started the task, this thread should not log.
            {
                this._logger.LogError(e, "Initializing device failed.");
            }
            success = false;
        }
        try
        {
            this._readerWriterLockSlim.EnterWriteLock();
            this._initializationTasks.Remove(adapterName);
            if (success)
            {
                this._initializedAdapters.Add(adapterName);
            }
        }
        finally
        {
            this._readerWriterLockSlim.ExitWriteLock();
        }
    }

    private static class OptionConstants
    {
        public const int MaxSearchResults = 10;
    }
}