using System;
using System.Collections.Generic;
using System.Linq;
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
    IReadOnlyCollection<IDeviceAdapter> Adapters { get; }

    ValueTask<IDeviceAdapter> GetAdapterAsync(string adapterName);

    IDeviceModel? GetDeviceByAdapterName(string adapterName);

    IDeviceModel? GetDeviceById(int id);

    ValueTask InitializeDeviceAsync(IDeviceAdapter adapter);

    SearchEntry<IDeviceModel>[] Search(string? query);
}

internal sealed class DeviceDatabase : IDeviceDatabase
{
    private readonly Dictionary<string, IDeviceAdapter> _adapters;
    private readonly TokenSearch<IDeviceModel> _deviceIndex;
    private readonly DeviceModel[] _devices;
    private readonly Dictionary<string, Task> _initializationTasks = new();
    private readonly HashSet<string> _initializedAdapters = new();
    private readonly ILogger<IDeviceDatabase> _logger;
    private readonly INotificationService _notificationService;

    public DeviceDatabase(SdkConfiguration sdkConfiguration, INotificationService notificationService, ILogger<IDeviceDatabase> logger)
    {
        this._notificationService = notificationService;
        this._logger = logger;
        this._devices = new DeviceModel[sdkConfiguration.Devices.Length];
        this._adapters = new(this._devices.Length);
        for (int index = 0; index < sdkConfiguration.Devices.Length; index++)
        {
            IDeviceBuilder device = sdkConfiguration.Devices[index];
            IDeviceAdapter adapter = device.BuildAdapter();
            this._adapters.Add(adapter.AdapterName, adapter);
            this._devices[index] = new(index, adapter);
            if (device.NotifierCallback is { } callback)
            {
                callback(new DeviceNotifier(this._notificationService, device.AdapterName, device.HasPowerStateSensor));
            }
        }
        this._deviceIndex = new(
            nameof(IDeviceModel.Manufacturer),
            nameof(IDeviceModel.Name),
            nameof(IDeviceModel.Tokens),
            nameof(IDeviceModel.Type)
        );
    }

    IReadOnlyCollection<IDeviceAdapter> IDeviceDatabase.Adapters => this._adapters.Values;

    public async ValueTask<IDeviceAdapter> GetAdapterAsync(string adapterName)
    {
        if (adapterName == null || !this._adapters.TryGetValue(adapterName, out IDeviceAdapter? adapter))
        {
            throw new ArgumentException($"No matching adapter with name \"{adapterName}\".", nameof(adapterName));
        }
        await this.InitializeDeviceAsync(adapter).ConfigureAwait(false);
        return adapter;
    }

    public IDeviceModel? GetDeviceByAdapterName(string name) => this._devices.FirstOrDefault(device => device.AdapterName == name);

    public IDeviceModel? GetDeviceById(int id) => id >= 0 && id < this._devices.Length
        ? this._devices[id]
        : null;

    public async ValueTask InitializeDeviceAsync(IDeviceAdapter adapter)
    {
        if (this._initializedAdapters.Contains(adapter.AdapterName) || adapter.Initializer is null)
        {
            return;
        }
        if (this._initializationTasks.TryGetValue(adapter.AdapterName, out Task? task))
        {
            await task.ConfigureAwait(false);
            return;
        }
        try
        {
            this._logger.LogInformation("Initializing device: {name}", adapter.AdapterName);
            this._initializationTasks.Add(adapter.AdapterName, task = adapter.Initializer());
            await task.ConfigureAwait(false);
            this._initializedAdapters.Add(adapter.AdapterName);
        }
        catch (Exception e)
        {
            this._logger.LogError("Initializing device failed: {message}", e.Message);
        }
        finally
        {
            this._initializationTasks.Remove(adapter.AdapterName);
        }
    }

    public SearchEntry<IDeviceModel>[] Search(string? query) => string.IsNullOrEmpty(query)
        ? Array.Empty<SearchEntry<IDeviceModel>>()
        : this._deviceIndex.Search(this._devices, query).Take(OptionConstants.MaxSearchResults).ToArray();

    private static class OptionConstants
    {
        public const int MaxSearchResults = 10;
    }
}