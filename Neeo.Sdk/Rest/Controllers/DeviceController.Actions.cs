using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/{componentName}/{deviceId}")]
    public async Task<ActionResult> GetValueAsync(string adapterName, string componentName, string deviceId)
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId) is not ({ } adapter, { } feature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Get {type}:{component} on {name}:{id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IButtonFeature buttonFeature => this.Ok(await buttonFeature.ExecuteAsync(deviceId)),
            IValueFeature valueFeature => this.Ok(await valueFeature.GetValueAsync(deviceId)),
            _ => this.NotFound(),
        };
    }

    [HttpPost("{adapterName}/{componentName}/{deviceId}")]
    public async Task<ActionResult> GetValueAsync(string adapterName, string componentName, string deviceId, [FromBody] JsonElement parameters)
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId) is not ({ } adapter, { } feature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Get {type}:{component} on {name}:{id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IFavoritesFeature favoritesFeature => this.Ok(await favoritesFeature.ExecuteAsync(deviceId, parameters.Deserialize<FavoriteData>().FavoriteId)),
            IDirectoryFeature directoryFeature => JsonSerialization.Ok(await directoryFeature.BrowseAsync(deviceId, parameters.Deserialize<BrowseParameters>())),
            _ => this.NotFound(),
        };
    }

    [HttpPost("{adapterName}/{componentName}/{deviceId}/action")]
    public async Task<ActionResult<SuccessResponse>> PerformActionAsync(string adapterName, string componentName, string deviceId, [FromBody] ActionData action)
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId) is not ({ } adapter, IDirectoryFeature directoryFeature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Perform directory action {action} on {name}:{id}", action.ActionIdentifier, adapter.DeviceName, deviceId);
        return this.Ok(await directoryFeature.PerformActionAsync(deviceId, action.ActionIdentifier));
    }

    [HttpGet("{adapterName}/{componentName}/{deviceId}/{value}")]
    public async Task<ActionResult<SuccessResponse>> SetValueAsync(string adapterName, string componentName, string deviceId, string value)
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId) is not ({ } adapter, IValueFeature valueFeature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Set {component} value to {value} on {name}:{id}", componentName, value, adapter.DeviceName, deviceId);
        return this.Ok(await valueFeature.SetValueAsync(deviceId, value));
    }

    private async ValueTask<(IDeviceAdapter, IFeature)> TryResolveAsync(string adapterName, string componentName, string deviceId)
    {
        if (await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false) is not { } adapter)
        {
            return default;
        }
        if (adapter.GetFeature(componentName) is { } feature)
        {
            // Static device component.
            return (adapter, feature);
        }
        // It may be from a dynamic device.
        return await this._dynamicDevices.GetDiscoveredDeviceAsync(adapter, deviceId) is { } dynamicDeviceAdapter && dynamicDeviceAdapter.GetFeature(componentName) is { } dynamicDeviceFeature
            ? (dynamicDeviceAdapter, dynamicDeviceFeature)
            : default;
    }

    public record struct ActionData(string ActionIdentifier);

    private record struct FavoriteData(string FavoriteId);
}