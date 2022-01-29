using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;

namespace Neeo.Sdk.Rest;

public interface IDynamicDevices : IDynamicDeviceRegistrar
{
    ValueTask<bool> StoreDiscoveryHandlerInRequestAsync(HttpContext context, string deviceId, object placeholder);

    void StorePlaceholderInRequest(HttpContext context, IDeviceAdapter adapter, string text);

    bool TryGetPlaceholder(HttpContext context, [NotNullWhen(true)] out object? placeholder);
}

internal sealed class DynamicDevices : IDynamicDevices
{
    private readonly Dictionary<string, IDeviceAdapter> _discoveredDynamicDevices = new();
    private readonly ILogger<DynamicDevices> _logger;

    public DynamicDevices(ILogger<DynamicDevices> logger) => this._logger = logger;

    public void RegisterDiscoveredDevice(string deviceId, IDiscoveryFeature controller)
    {
    }

    public void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter)
    {
        this._discoveredDynamicDevices.Add(deviceId, adapter);
        this._logger.LogInformation("Added device, currently registered {count} dynamic device.", this._discoveredDynamicDevices.Count);
    }

    public async ValueTask<bool> StoreDiscoveryHandlerInRequestAsync(HttpContext httpContext, string deviceId, object placeholder)
    {
        (IDeviceAdapter adapter, string componentName) = (DynamicPlaceholder)placeholder;
        if (TryStore())
        {
            return true;
        }
        if (adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature discoveryController)
        {
            this._logger.LogWarning("No discovery component found.");
            return false;
        }
        await discoveryController.DiscoverAsync(deviceId).ConfigureAwait(false);
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

    public void StorePlaceholderInRequest(HttpContext context, IDeviceAdapter adapter, string componentName) => context.SetItem(new DynamicPlaceholder(adapter, componentName));

    public bool TryGetPlaceholder(HttpContext context, [NotNullWhen(true)] out object? placeholder) => context.Items.TryGetValue(nameof(DynamicPlaceholder), out placeholder);

    private sealed record class DynamicPlaceholder(IDeviceAdapter Adapter, string ComponentName);
}