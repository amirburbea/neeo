using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Features;

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
        logger.LogInformation("Execute {Type}:{Component} on {Name}:{Id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IFavoritesFeature favoritesFeature => this.Ok(await favoritesFeature.ExecuteAsync(
                deviceId,
                parameters.Deserialize<FavoritePayload>(JsonSerializerOptions.Web).FavoriteId,
                cancellationToken
            )),
            IDirectoryFeature directoryFeature => this.Ok(await directoryFeature.BrowseAsync(
                deviceId,
                parameters.Deserialize<BrowseParameters>(JsonSerializerOptions.Web),
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
        logger.LogInformation("Get {Type}:{Component} on {Name}:{Id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IButtonFeature buttonFeature => this.Ok(await buttonFeature.ExecuteAsync(
                deviceId,
                cancellationToken
            )),
            IValueFeature valueFeature => this.Ok(await valueFeature.GetValueAsync(
                deviceId,
                cancellationToken
            )),
            _ => this.NotFound(),
        };
    }

    [HttpPost("{adapterName}/{componentName}/{deviceId}/action")]
    public async Task<ActionResult<SuccessResponse>> PerformDirectoryActionAsync(
        string adapterName,
        string componentName,
        string deviceId,
        [FromBody] DirectoryActionPayload action,
        CancellationToken cancellationToken
    )
    {
        if (await this.TryResolveAsync(adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, IDirectoryFeature directoryFeature))
        {
            return this.NotFound();
        }
        logger.LogInformation("Perform directory action {Action} on {Name}:{Id}", action.ActionIdentifier, adapter.DeviceName, deviceId);
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
        logger.LogInformation("Set {Component} value to {Value} on {Name}:{Id}", componentName, value, adapter.DeviceName, deviceId);
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

    public readonly record struct DirectoryActionPayload(string ActionIdentifier);

    public readonly record struct FavoritePayload(string FavoriteId);
}
