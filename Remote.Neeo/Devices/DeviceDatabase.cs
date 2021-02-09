using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Remote.Neeo.Devices
{
    public interface IDeviceDatabase
    {
        Task<IDeviceAdapter> GetAdapterAsync(string adapterName);

        IEnumerable<SearchResult<IDeviceModel>> Search(string? query);
    }

    internal sealed class DeviceDatabase : IDeviceDatabase
    {
        private readonly IReadOnlyDictionary<string, IDeviceAdapter> _adapters;
        private readonly TokenSearch<IDeviceModel> _deviceIndex;
        private readonly List<IDeviceModel> _devices;
        private readonly HashSet<string> _initializedAdapterss;
        private readonly ILogger<IDeviceDatabase> _logger;

        public DeviceDatabase(IReadOnlyCollection<IDeviceAdapter> adapters, ILogger<IDeviceDatabase> logger)
        {
            this._adapters = adapters.ToDictionary(adapter => adapter.AdapterName);
            this._devices = new();
            this._deviceIndex = new(this._devices,  nameof(IDeviceModel.Manufacturer), nameof(IDeviceModel.Name), nameof(IDeviceModel.Type), nameof(IDeviceModel.Tokens))
            {
                Delimiter = Constants.Delimiter,
                Threshold = Constants.MatchFactor
            };
            this._initializedAdapterss = new();
            this._logger = logger;
        }

        public async Task<IDeviceAdapter> GetAdapterAsync(string adapterName)
        {
            if (string.IsNullOrEmpty(adapterName) || !this._adapters.TryGetValue(adapterName, out IDeviceAdapter? adapter))
            {
                throw new ArgumentException("No matching adapter name.", nameof(adapterName));
            }
            await this.InitializeAsync(adapter).ConfigureAwait(false);
            return adapter;
        }

        public IEnumerable<SearchResult<IDeviceModel>> Search(string? query)
        {
            if (String.IsNullOrEmpty(query))
            {
                return Array.Empty<SearchResult<IDeviceModel>>();
            }
            IReadOnlyCollection<SearchResult<IDeviceModel>> results = this._deviceIndex.Search(query);
            return results.Count > Constants.MaxSearchResults ? results.Take(Constants.MaxSearchResults) : results;
        }

        private async Task InitializeAsync(IDeviceAdapter adapter)
        {
            if (!this._initializedAdapterss.Add(adapter.AdapterName) || adapter.Initializer == null)
            {
                return;
            }
            try
            {
                await adapter.Initializer.InitializeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this._logger.LogError("Initializing device failed: {message}", e.Message);
                this._initializedAdapterss.Remove(adapter.AdapterName);
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
