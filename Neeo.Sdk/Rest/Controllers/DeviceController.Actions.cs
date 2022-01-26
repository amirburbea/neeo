using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;
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
        if (this.HttpContext.GetItem<IController>() is not { } controller)
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            return this.NotFound();
        }
        else if (controller is IValueController valueController)
        {
            return this.Ok(new ValueResult(await valueController.GetValueAsync(deviceId)));
        }
        else if (controller is IButtonController buttonController)
        {
            await buttonController.TriggerAsync(deviceId);
            return this.Ok(new SuccessResult());
        }
        return this.BadRequest();
    }

    [HttpPost("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult<object>> GetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId,
        [FromBody] object value
    )
    {
        if (this.HttpContext.GetItem<IController>() is not { } controller)
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            throw new();
        }
        switch (controller)
        {
            case IDirectoryController directoryController:
                break;
            case IFavoritesController favoritesController when value is JsonElement favoriteData:
                await favoritesController.ExecuteAsync(deviceId, favoriteData.Deserialize<FavoriteData>(JsonSerialization.Options).FavoriteId);
                return this.Ok(new SuccessResult());
        }
        return this.BadRequest();
    }

    [HttpPost("{adapter}/{componentName}/{deviceId}/action")]
    public async Task<ActionResult<object>> PerformActionAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId,
        [FromBody] object value
    )
    {
        if (this.HttpContext.GetItem<IController>() is not IDirectoryController controller)
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            return this.NotFound();
        }
        await Task.Delay(0);
        return this.NotFound();
    }

    [HttpGet("{adapter}/{componentName}/{deviceId}/{value}")]
    public async Task<ActionResult<SuccessResult>> SetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(DeviceIdBinder))] string deviceId,
        string value
    )
    {
        if (this.HttpContext.GetItem<IController>() is not IValueController controller)
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            return this.NotFound();
        }
        await controller.SetValueAsync(deviceId, value);
        return new SuccessResult();
    }

    private record struct FavoriteData(string FavoriteId);
}