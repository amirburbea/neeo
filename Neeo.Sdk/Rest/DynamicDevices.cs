using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest;

public interface IDynamicDevices : IDynamicDeviceRegistrar
{
    ValueTask<bool> StoreDiscoveryHandlerInRequestAsync(HttpContext context, string deviceId, object placeholder);

    void StorePlaceholderInRequest(HttpContext context, string text);

    bool TryGetPlaceholder(HttpContext context, [NotNullWhen(true)] out object? placeholder);
}

internal sealed class DynamicDevices : IDynamicDevices
{
    private readonly Dictionary<string, IDeviceAdapter> _discoveredDynamicDevices = new();
    private readonly ILogger<DynamicDevices> _logger;

    public DynamicDevices(ILogger<DynamicDevices> logger) => this._logger = logger;

    public void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter)
    {
        this._discoveredDynamicDevices.Add(deviceId, adapter);
        this._logger.LogInformation("Added device, currently registered {count} dynamic device.", this._discoveredDynamicDevices.Count);
    }

    public async ValueTask<bool> StoreDiscoveryHandlerInRequestAsync(HttpContext httpContext, string deviceId, object placeholder)
    {
        IDeviceAdapter adapter = httpContext.GetItem<IDeviceAdapter>()!;
        string componentName = (string)placeholder;
        if (TryStore())
        {
            return true;
        }
        if (adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature feature)
        {
            this._logger.LogWarning("No discovery component found.");
            return false;
        }
        await feature.DiscoverAsync(deviceId).ConfigureAwait(false);
        return TryStore();

        bool TryStore()
        {
            if (!this._discoveredDynamicDevices.TryGetValue(deviceId, out IDeviceAdapter? adapter) || adapter.GetFeature(componentName) is not { } feature)
            {
                return false;
            }
            httpContext.SetItem(feature);
            return true;
        }
    }

    public void StorePlaceholderInRequest(HttpContext context, string text) => context.SetItem( text);

    public bool TryGetPlaceholder(HttpContext context, [NotNullWhen(true)] out object? placeholder) => (placeholder = context.GetItem<string>()) is not null;

    private sealed record class DynamicPlaceholder(IDeviceAdapter Adapter, string ComponentName);
}