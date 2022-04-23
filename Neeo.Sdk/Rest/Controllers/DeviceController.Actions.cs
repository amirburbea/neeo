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
            IButtonFeature buttonFeature => JsonSerialization.Ok(await buttonFeature.ExecuteAsync(deviceId)),
            IValueFeature valueFeature => JsonSerialization.Ok(await valueFeature.GetValueAsync(deviceId)),
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
            IFavoritesFeature favoritesFeature => JsonSerialization.Ok(await favoritesFeature.ExecuteAsync(deviceId, parameters.Deserialize<FavoriteData>().FavoriteId)),
            IDirectoryFeature directoryFeature => JsonSerialization.Ok(await directoryFeature.BrowseAsync(deviceId, parameters.Deserialize<BrowseParameters>())),
            _ => this.NotFound(),
        };
    }

    [HttpPost("{adapterName}/{componentName}/{deviceId}/action")]
    public async Task<ActionResult<SuccessResponse>> PerformActionAsync(string adapterName, string componentName, string deviceId, [FromBody] DirectoryAction action)
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId) is not ({ } adapter, IDirectoryFeature directoryFeature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Perform directory action {action} on {name}:{id}", action.ActionIdentifier, adapter.DeviceName, deviceId);
        return await directoryFeature.PerformActionAsync(deviceId, action.ActionIdentifier);
    }

    [HttpGet("{adapterName}/{componentName}/{deviceId}/{value}")]
    public async Task<ActionResult<SuccessResponse>> SetValueAsync(string adapterName, string componentName, string deviceId, string value)
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId) is not ({ } adapter, IValueFeature valueFeature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Set {component} value to {value} on {name}:{id}", componentName, value, adapter.DeviceName, deviceId);
        return await valueFeature.SetValueAsync(deviceId, value);
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
        // Check for a discovered device with `EnableDynamicDeviceBuilder`.
        if (await this._dynamicDevices.GetDiscoveredDeviceAsync(adapter, deviceId).ConfigureAwait(false) is not { } dynamicDeviceAdapter)
        {
            return default;
        }
        // Check that the dynamic device has the feature.
        return dynamicDeviceAdapter.GetFeature(componentName) is { } dynamicDeviceFeature
            ? (dynamicDeviceAdapter, dynamicDeviceFeature)
            : default;
    }

    public record struct DirectoryAction(string ActionIdentifier);

    private record struct FavoriteData(string FavoriteId);
}