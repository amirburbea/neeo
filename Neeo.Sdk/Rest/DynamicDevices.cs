using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;

namespace Neeo.Sdk.Rest;

public interface IDynamicDevices : IDynamicDeviceRegistrar
{
    bool HasPlaceholder(HttpContext context);

    ValueTask<bool> StoreDiscoveryHandlerInRequestAsync(HttpContext context, string deviceId);

    void StorePlaceholderInRequest(HttpContext context, IDeviceAdapter adapter, string text);
}

internal sealed class DynamicDevices : IDynamicDevices
{
    private readonly Dictionary<string, IDeviceAdapter> _discoveredDynamicDevices = new();
    private readonly ILogger<DynamicDevices> _logger;

    public DynamicDevices(ILogger<DynamicDevices> logger) => this._logger = logger;

    public bool HasPlaceholder(HttpContext context) => context.HasItem<DynamicPlaceholder>();

    public void RegisterDiscoveredDevice(string deviceId, IDiscoveryController controller)
    {
    }

    public void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter)
    {
        this._discoveredDynamicDevices.Add(deviceId, adapter);
        this._logger.LogInformation("Added device, currently registered {count} dynamic device.", this._discoveredDynamicDevices.Count);
    }

    public async ValueTask<bool> StoreDiscoveryHandlerInRequestAsync(HttpContext httpContext, string deviceId)
    {
        (IDeviceAdapter adapter, string componentName) = httpContext.GetItem<DynamicPlaceholder>()!;
        if (TryStore())
        {
            return true;
        }
        if (adapter.GetCapabilityHandler(ComponentType.Discovery) is not IDiscoveryController discoveryController)
        {
            this._logger.LogWarning("No discovery component found.");
            return false;
        }
        await discoveryController.DiscoverAsync(deviceId).ConfigureAwait(false);
        return TryStore();

        bool TryStore()
        {
            if (!this._discoveredDynamicDevices.TryGetValue(deviceId, out IDeviceAdapter? adapter) || adapter.GetCapabilityHandler(componentName) is not { } controller)
            {
                return false;
            }
            httpContext.SetItem(controller);
            return true;
        }
    }

    public void StorePlaceholderInRequest(HttpContext context, IDeviceAdapter adapter, string componentName) => context.SetItem(new DynamicPlaceholder(adapter, componentName));

    private record DynamicPlaceholder(IDeviceAdapter Adapter, string ComponentName);
}