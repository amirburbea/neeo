using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Notifications;
using Neeo.Api.Utilities.TokenSearch;

namespace Neeo.Api.Devices;

/// <summary>
/// Interface for a device database.
/// </summary>
public interface IDeviceDatabase
{
    IReadOnlyCollection<IDeviceAdapter> Adapters { get; }

    ValueTask<IDeviceAdapter> GetAdapterAsync(string adapterName);

    IDeviceModel? GetDeviceByAdapterName(string adapterName);

    IDeviceModel? GetDeviceById(int id);

    ISearchItem<IDeviceModel>[] Search(string? query);
}

internal sealed class DeviceDatabase : IDeviceDatabase
{
    private readonly Dictionary<string, IDeviceAdapter> _adapters = new();
    private readonly TokenSearch<IDeviceModel> _deviceIndex;
    private readonly List<DeviceModel> _devices = new();
    private readonly Dictionary<string, Task> _initializationTasks = new();
    private readonly HashSet<string> _initializedAdapters = new();
    private readonly ILogger<IDeviceDatabase> _logger;
    private readonly INotificationService _notificationService;

    public DeviceDatabase(
        DiscoveryControllerFactory discoveryControllerFactory,
        IReadOnlyCollection<IDeviceBuilder> deviceBuilders,
        INotificationService notificationService,
        ILogger<IDeviceDatabase> logger
    )
    {
        this._notificationService = notificationService;
        this._logger = logger;
        foreach (IDeviceBuilder device in deviceBuilders)
        {
            IDeviceAdapter adapter = DeviceAdapter.Build(device, discoveryControllerFactory);
            if (device.NotifierCallback is { } callback)
            {
                callback(new DeviceNotifier(this._notificationService,device.AdapterName, device.HasPowerStateSensor ));
            }
            this._adapters.Add(adapter.AdapterName, adapter);
            this._devices.Add(new(this._devices.Count, adapter));
        }
        this._deviceIndex = new(new()
        {
            SearchProperties = new[]
            {
                    nameof(IDeviceModel.Manufacturer),
                    nameof(IDeviceModel.Name),
                    nameof(IDeviceModel.Tokens),
                    nameof(IDeviceModel.Type),
            },
            Threshold = OptionConstants.MatchFactor,
            Delimiter = new[] { OptionConstants.Delimiter }
        });
    }

    IReadOnlyCollection<IDeviceAdapter> IDeviceDatabase.Adapters => this._adapters.Values;

    public async ValueTask<IDeviceAdapter> GetAdapterAsync(string adapterName)
    {
        if (adapterName == null || !this._adapters.TryGetValue(adapterName, out IDeviceAdapter? adapter))
        {
            throw new ArgumentException($"No matching adapter with name \"{adapterName}\".", nameof(adapterName));
        }
        await this.InitializeAsync(adapter).ConfigureAwait(false);
        return adapter;
    }

    public IDeviceModel? GetDeviceByAdapterName(string name) => this._devices.FirstOrDefault(device => device.AdapterName == name);

    public IDeviceModel? GetDeviceById(int id) => id >= 0 && id < this._devices.Count
        ? this._devices[id]
        : null;

    public ISearchItem<IDeviceModel>[] Search(string? query) => string.IsNullOrEmpty(query)
        ? Array.Empty<ISearchItem<IDeviceModel>>()
        : this._deviceIndex.Search(this._devices, query).Take(OptionConstants.MaxSearchResults).ToArray();

    private async ValueTask InitializeAsync(IDeviceAdapter adapter)
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

    private static class OptionConstants
    {
        public const char Delimiter = ' ';
        public const double MatchFactor = 0.5;
        public const int MaxSearchResults = 10;
    }
}
