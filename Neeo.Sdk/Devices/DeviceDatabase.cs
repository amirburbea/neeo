using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Notifications;

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
    /// Get the adapter with the specified <paramref name="adapterName"/>. If the adapter has a
    /// registered initializer, ensures the adapter is initialized.
    /// </summary>
    /// <param name="adapterName">The name of the adapter.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests (defaults to <see cref="CancellationToken.None"/>).
    /// </param>
    /// <returns><see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the associated model for an adapter with the specified <paramref name="adapterName"/>,
    /// or <see langword="null"/> if not found.
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
    /// <returns>An array of sort entries ranked as per a similar algorithm to "tokenseach.js".</returns>
    DeviceSearchResult[] Search(string? query);
}

internal sealed class DeviceDatabase : IDeviceDatabase
{
    private readonly Dictionary<string, DeviceAdapterContainer> _containers;
    private readonly DeviceIndex _deviceIndex;
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
        this._deviceIndex = new(this._devices);
    }

    public IEnumerable<IDeviceAdapter> Adapters => from container in this._containers.Values select container.Adapter;

    public async ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName, CancellationToken cancellationToken = default)
    {
        if (this._containers.GetValueOrDefault(adapterName ?? throw new ArgumentNullException(nameof(adapterName))) is not { } container)
        {
            return null;
        }
        if (container.IsUnitialized)
        {
            await container.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        return container.Adapter;
    }

    public DeviceModel? GetDeviceByAdapterName(string name)
    {
        return Array.Find(this._devices, device => device.AdapterName == name);
    }

    public DeviceModel? GetDeviceById(int id)
    {
        return id is > -1 && id < this._devices.Length ? this._devices[id] : null;
    }

    public DeviceSearchResult[] Search(string? query) => string.IsNullOrEmpty(query)
        ? []
        : [.. this._deviceIndex.Search(query).Take(OptionConstants.MaxSearchResults)];

    /// <summary>
    /// A class to search a collection of items by tokens and return ranked results. Based on <a href="https://github.com/neophob/tokensearch.js">tokensearch.js</a>.
    /// </summary>
    internal sealed class DeviceIndex(DeviceModel[] devices)
    {
        public IEnumerable<DeviceSearchResult> Search(string query)
        {
            string[] searchTokens = [..
                (query ?? throw new ArgumentNullException(nameof(query)))
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .Take(5)
            ];
            List<SearchEntry> entries = [];
            int maxScore = 0;
            foreach (DeviceModel device in devices)
            {
                string[] dataTokens =
                [
                    device.Manufacturer,
                    device.Name,
                    device.Tokens,
                    Enum.GetName(device.Type)!
                ];
                int score = dataTokens.Sum(dataToken =>
                {
                    return searchTokens.Sum(ScoreSearchToken);

                    int ScoreSearchToken(string searchToken) => DeviceIndex.Score(dataToken, searchToken);
                });
                if (score <= 0)
                {
                    continue;
                }
                maxScore = Math.Max(score, maxScore);
                entries.Add(new(device) { Score = score });
            }
            return DeviceIndex.Normalize(entries, maxScore)
                .OrderBy(entry => entry.Score, Comparer<double>.Default)
                .ThenBy(entry => entry.Device.Name, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new DeviceSearchResult(entry.Device, entry.Score, maxScore));
        }

        private static int Score(string text, string searchToken)
        {
            int index = text.IndexOf(searchToken, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                return 0;
            }
            if (searchToken.Length == 1 || index is not 0)
            {
                return 1;
            }
            return text.Length == searchToken.Length ? 6 : 2;
        }

        private static IEnumerable<SearchEntry> Normalize(IEnumerable<SearchEntry> entries, int maxScore)
        {
            double normalizedScore = 1d / maxScore;
            HashSet<string> hashSet = new(StringComparer.OrdinalIgnoreCase);
            foreach (SearchEntry entry in entries)
            {
                entry.Score = 1d - entry.Score * normalizedScore;
                string key = $"{entry.Device.Manufacturer}|{entry.Device.Name}|{entry.Device.Tokens}|{Enum.GetName(entry.Device.Type)}";
                if (entry.Score <= 0.5 && hashSet.Add(key))
                {
                    yield return entry;
                }
            }
        }
    }

    private static class OptionConstants
    {
        public const int MaxSearchResults = 10;
    }

    private sealed class DeviceAdapterContainer(IDeviceAdapter adapter, ILogger logger)
    {
        private readonly Lock _lock = new();
        private Task? _initializationTask;

        public IDeviceAdapter Adapter => adapter;

        public bool IsUnitialized => adapter.Initializer is { } && this._initializationTask is not { IsCompletedSuccessfully: true };

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (adapter.Initializer is not { } initializer)
            {
                return Task.CompletedTask;
            }
            if (this._initializationTask is not null)
            {
                return this._initializationTask;
            }
            using Lock.Scope scope = this._lock.EnterScope();
            return this._initializationTask ??= InitializeAsync();

            async Task InitializeAsync()
            {
                logger.LogInformation("Initializing adapter {DeviceName} ({AdapterName})...", adapter.DeviceName, adapter.AdapterName);
                try
                {
                    await initializer(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error initializing adapter.");
                }
            }
        }
    }

    private sealed class SearchEntry(DeviceModel device)
    {
        public DeviceModel Device => device;

        public double Score { get; set; }
    }
}
