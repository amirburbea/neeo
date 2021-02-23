using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remote.Utilities.TokenSearch;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Interface for a device database.
    /// </summary>
    public interface IDeviceDatabase
    {
        Task<IDeviceAdapter> GetAdapterAsync(string adapterName);

        IDeviceModel GetDevice(int id);

        IDeviceModel GetDeviceByAdapterName(string adapterName);

        IEnumerable<SearchItem<IDeviceModel>> Search(string? query);
    }

    internal sealed class DeviceDatabase : IDeviceDatabase
    {
        private readonly Dictionary<string, IDeviceAdapter> _adapters = new();
        private readonly TokenSearch<IDeviceModel> _deviceIndex;
        private readonly List<IDeviceModel> _devices;
        private readonly HashSet<string> _initializedAdapters;
        private readonly ILogger<IDeviceDatabase> _logger;

        public DeviceDatabase(IReadOnlyCollection<IDeviceAdapter> adapters, ILogger<IDeviceDatabase> logger)
        {
            this._adapters = adapters.ToDictionary(adapter => adapter.AdapterName);
            this._initializedAdapters = new();
            this._logger = logger;
            int id = 0;
            this._devices = new(
                from adapter in adapters
                from device in adapter.Devices
                select new DeviceModel(
                    id++,
                    adapter.AdapterName,
                    adapter.Type,
                    device.Name,
                    adapter.DriverVersion,
                    adapter.Manufacturer,
                    string.Join(' ', device.Tokens)
                )
            );
            this._deviceIndex = new(this._devices, new()
            {
                SearchProperties = new[] { nameof(IDeviceModel.Manufacturer), nameof(IDeviceModel.Name), nameof(IDeviceModel.Type), nameof(IDeviceModel.Tokens) },
                Threshold = Constants.MatchFactor,
                Delimiter = new[] { Constants.Delimiter },
                Unique = true,
                SortAlgorithm = (left, right) =>
                {
                    int comparison = left.Score.CompareTo(right.Score);
                    return comparison != 0 ? comparison : StringComparer.OrdinalIgnoreCase.Compare(left.Item.Name, right.Item.Name);
                }
            });
        }

        public async Task<IDeviceAdapter> GetAdapterAsync(string adapterName)
        {
            if (string.IsNullOrEmpty(adapterName) || !this._adapters.TryGetValue(adapterName, out IDeviceAdapter? adapter))
            {
                throw new ArgumentException($"No matching adapter with name \"{adapterName}\".", nameof(adapterName));
            }
            await this.InitializeAsync(adapter).ConfigureAwait(false);
            return adapter;
        }

        public IDeviceModel GetDevice(int id)
        {
            return id >= 0 && id < this._devices.Count
                ? this._devices[id]
                : throw new ArgumentException($"No matching device with id {id}.", nameof(id));
        }

        public IDeviceModel GetDeviceByAdapterName(string name)
        {
            return this._devices.FirstOrDefault(device => device.AdapterName == name) is DeviceModel device
                ? device
                : throw new ArgumentException($"No matching device with adapter name \"{name}\".", nameof(name));
        }

        public IEnumerable<SearchItem<IDeviceModel>> Search(string? query)
        {
            return string.IsNullOrEmpty(query)                
                ? Enumerable.Empty<SearchItem<IDeviceModel>>()                
                : this._deviceIndex.Search(query).Take(Constants.MaxSearchResults);
        }

        private async Task InitializeAsync(IDeviceAdapter adapter)
        {
            if (adapter.Initializer == null)
            {
                this._initializedAdapters.Add(adapter.AdapterName);
                return;
            }
            else if (this._initializedAdapters.Contains(adapter.AdapterName))
            {
                return;
            }
            try
            {
                await adapter.Initializer().ConfigureAwait(false);
                this._initializedAdapters.Add(adapter.AdapterName);
            }
            catch (Exception e)
            {
                this._logger.LogError("Initializing device failed: {message}", e.Message);
            }
        }

        private static class Constants
        {
            public const char Delimiter = ' ';
            public const double MatchFactor = 0.5;
            public const int MaxSearchResults = 10;
        }
    }
}