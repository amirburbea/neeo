using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Json;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult<object>> GetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId
    )
    {
        if (!this.TryGetFeature(adapter, componentName, out IFeature? feature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Get {type}:{component} on {name}:{id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature.Type switch
        {
            FeatureType.Button => this.Ok(await DeviceController.ExecuteAsync(((IButtonFeature)feature).TriggerAsync(deviceId))), // SuccessResponse,
            FeatureType.Value => this.Ok(new ValueResponse(await ((IValueFeature)feature).GetValueAsync(deviceId))), // ValueResponse
            _ => this.BadRequest()
        };
    }

    [HttpPost("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult> GetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId,
        [FromBody] JsonElement parameters
    )
    {
        if (!this.TryGetFeature(adapter, componentName, out IFeature? feature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Get {type}:{component} on {name}:{id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature.Type switch
        {
            FeatureType.Favorites => this.Ok(await DeviceController.ExecuteAsync(((IFavoritesFeature)feature).ExecuteAsync(deviceId, parameters.Deserialize<FavoriteData>().FavoriteId))),
            FeatureType.Directory => this.Ok<IListBuilder>(await ((IDirectoryFeature)feature).BrowseAsync(deviceId, parameters.Deserialize<ListParameters>())),
            _ => this.BadRequest()
        };
    }

    [HttpPost("{adapter}/{componentName}/{deviceId}/action")]
    public async Task<ActionResult> PerformActionAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId,
        [FromBody] ActionData action
    )
    {
        if (!this.TryGetFeature(adapter, componentName, out IDirectoryFeature? feature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Perform directory action {action} on {name}:{id}", action.ActionIdentifier, adapter.DeviceName, deviceId);
        await feature.PerformActionAsync(deviceId, action.ActionIdentifier);
        return this.Ok();
    }

    [HttpGet("{adapter}/{componentName}/{deviceId}/{value}")]
    public async Task<ActionResult<SuccessResponse>> SetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId,
        string value
    )
    {
        if (!this.TryGetFeature(adapter, componentName, out IValueFeature? feature))
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Set {component} value to {value} on {name}:{id}", componentName, value, adapter.DeviceName, deviceId);
        return this.Ok(await DeviceController.ExecuteAsync(feature.SetValueAsync(deviceId, value)));
    }

    private bool TryGetFeature<TFeature>(IDeviceAdapter adapter, string componentName, [NotNullWhen(true)] out TFeature? feature)
        where TFeature : IFeature
    {
        if (this.HttpContext.GetItem<IFeature>() is TFeature item)
        {
            feature = item;
            return true;
        }
        this._logger.LogError("Can not perform action as the component ({component}) was not found on '{device}'.", componentName, adapter.DeviceName);
        feature = default;
        return false;
    }

    private static Task<SuccessResponse> ExecuteAsync(Task task) => task.ContinueWith(
        _ => new SuccessResponse(true),
        TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously
    );

    public record struct ActionData(string ActionIdentifier);

    private record struct FavoriteData(string FavoriteId);
}