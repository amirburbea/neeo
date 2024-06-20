using System.Text.Json;
using System.Threading;
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
    [HttpPost("{adapterName}/{componentName}/{deviceId}")]
    public async Task<ActionResult> ExecuteAsync(
        string adapterName,
        string componentName,
        string deviceId,
        [FromBody] JsonElement parameters,
        CancellationToken cancellationToken
    )
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, { } feature))
        {
            return this.NotFound();
        }
        logger.LogInformation("Execute {type}:{component} on {name}:{id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IFavoritesFeature favoritesFeature => this.Ok(await favoritesFeature.ExecuteAsync(
                deviceId,
                parameters.GetProperty("favoriteId").GetString()!,
                cancellationToken
            )),
            IDirectoryFeature directoryFeature => this.Ok(await directoryFeature.BrowseAsync(
                deviceId,
                parameters.Deserialize<BrowseParameters>(JsonSerialization.Options),
                cancellationToken
            )),
            _ => this.NotFound(),
        };
    }

    [HttpGet("{adapterName}/{componentName}/{deviceId}")]
    public async Task<ActionResult> GetValueAsync(
        string adapterName,
        string componentName,
        string deviceId,
        CancellationToken cancellationToken
    )
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, { } feature))
        {
            return this.NotFound();
        }
        logger.LogInformation("Get {type}:{component} on {name}:{id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IButtonFeature buttonFeature => this.Ok(await buttonFeature.ExecuteAsync(deviceId, cancellationToken)),
            IValueFeature valueFeature => this.Ok(await valueFeature.GetValueAsync(deviceId, cancellationToken)),
            _ => this.NotFound(),
        };
    }

    [HttpPost("{adapterName}/{componentName}/{deviceId}/action")]
    public async Task<ActionResult<SuccessResponse>> PerformDirectoryActionAsync(
        string adapterName,
        string componentName,
        string deviceId,
        [FromBody] DirectoryAction action,
        CancellationToken cancellationToken
    )
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, IDirectoryFeature directoryFeature))
        {
            return this.NotFound();
        }
        logger.LogInformation("Perform directory action {action} on {name}:{id}", action.ActionIdentifier, adapter.DeviceName, deviceId);
        return await directoryFeature.PerformActionAsync(deviceId, action.ActionIdentifier, cancellationToken);
    }

    [HttpGet("{adapterName}/{componentName}/{deviceId}/{value}")]
    public async Task<ActionResult<SuccessResponse>> SetValueAsync(
        string adapterName,
        string componentName,
        string deviceId,
        string value,
        CancellationToken cancellationToken
    )
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, IValueFeature valueFeature))
        {
            return this.NotFound();
        }
        logger.LogInformation("Set {component} value to {value} on {name}:{id}", componentName, value, adapter.DeviceName, deviceId);
        return await valueFeature.SetValueAsync(deviceId, value, cancellationToken);
    }

    private async ValueTask<(IDeviceAdapter, IFeature)> TryResolveAsync(
        string adapterName,
        string componentName,
        string deviceId,
        CancellationToken cancellationToken
    )
    {
        if (await this.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter)
        {
            return default;
        }
        // Static device component.
        if (adapter.GetFeature(componentName) is { } feature)
        {
            return (adapter, feature);
        }
        // Check for a discovered device with the name `deviceId` and has a component named `componentName`.
        if (await dynamicDeviceRegistry.GetDiscoveredDeviceAsync(adapter, deviceId, cancellationToken) is { } dynamicAdapter && dynamicAdapter.GetFeature(componentName) is { } dynamicFeature)
        {
            return (dynamicAdapter, dynamicFeature);
        }
        return default;
    }

    public readonly record struct DirectoryAction(string ActionIdentifier);

    private readonly record struct Favorite(string FavoriteId);
}
