using System;
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
    /// Get the adapter with the specified <paramref name="adapterName"/>. If the adapter
    /// has a registered initializer, ensures the adapter is initialized.
    /// </summary>
    /// <param name="adapterName">The name of the adapter.</param>
    /// <returns><see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName);

    /// <summary>
    /// Gets the adapters supporting device routes.
    /// </summary>
    /// <remarks>Note that the adapters may not have run their associated initializer.</remarks>
    IEnumerable<IDeviceAdapter> GetAdaptersWithDeviceRoutes();

    /// <summary>
    /// Gets the adapters supporting device subscription.
    /// </summary>
    /// <remarks>Note that the adapters may not have run their associated initializer.</remarks>
    IEnumerable<IDeviceAdapter> GetAdaptersWithSubscription();

    /// <summary>
    /// Gets the associated device model for an adapter with the specified <paramref name="adapterName"/>, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="adapterName">The name of the device adapter.</param>
    /// <returns>The device model if it exists, <see langword="null"/> otherwise.</returns>
    DeviceAdapterModel? GetDeviceByAdapterName(string adapterName);

    /// <summary>
    /// Gets the device model with the specified <paramref name="id"/>.
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
                callback(new DeviceNotifier(this._notificationService, device.AdapterName, device.HasPowerStateSensor));
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

    public async ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName)
    {
        if (!this._adapters.TryGetValue(adapterName ?? throw new ArgumentNullException(nameof(adapterName)), out IDeviceAdapter? adapter))
        {
            return default;
        }
        await this.InitializeDeviceAsync(adapter).ConfigureAwait(false);
        return adapter;
    }

    public IEnumerable<IDeviceAdapter> GetAdaptersWithDeviceRoutes() => this._adapters.Values.Where(static adapter => adapter.RouteHandler is { });

    public IEnumerable<IDeviceAdapter> GetAdaptersWithSubscription() => this._adapters.Values.Where(static adapter => adapter.GetFeature(ComponentType.Subscription) is { });

    public DeviceAdapterModel? GetDeviceByAdapterName(string name) => Array.FindIndex(this._devices, device => device.AdapterName == name) is int index and > -1
        ? this._devices[index]
        : default(DeviceAdapterModel?);

    public DeviceAdapterModel? GetDeviceById(int id) => id >= 0 && id < this._devices.Length
        ? this._devices[id]
        : default(DeviceAdapterModel?);

    public SearchEntry<DeviceAdapterModel>[] Search(string? query) => string.IsNullOrEmpty(query)
        ? Array.Empty<SearchEntry<DeviceAdapterModel>>()
        : this._deviceIndex.Search(query).Take(OptionConstants.MaxSearchResults).ToArray();

    private async ValueTask InitializeDeviceAsync(IDeviceAdapter adapter)
    {
        if (adapter.Initializer is not { } initializer)
        {
            return;
        }
        Task? inProgressTask = default;
        try
        {
            this._readerWriterLockSlim.EnterReadLock();
            if (this._initializedAdapters.Contains(adapter.AdapterName))
            {
                return;
            }
            inProgressTask = this._initializationTasks.GetValueOrDefault(adapter.AdapterName);
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
        try
        {
            this._readerWriterLockSlim.EnterWriteLock();
            if (!this._initializationTasks.TryGetValue(adapter.AdapterName, out inProgressTask))
            {
                this._logger.LogInformation("Initializing device: {name}", adapter.AdapterName);
                this._initializationTasks.TryAdd(adapter.AdapterName, inProgressTask = initializer());
            }
        }
        finally
        {
            this._readerWriterLockSlim.ExitWriteLock();
        }
        bool initialized;
        try
        {
            await inProgressTask.ConfigureAwait(false);
            initialized = true;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Initializing device failed.");
            initialized = false;
        }
        try
        {
            this._readerWriterLockSlim.EnterWriteLock();
            this._initializationTasks.Remove(adapter.AdapterName);
            if (initialized)
            {
                this._initializedAdapters.Add(adapter.AdapterName);
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