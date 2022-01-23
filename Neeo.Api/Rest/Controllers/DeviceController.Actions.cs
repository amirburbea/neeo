using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpPost("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult<object>> PerformActionAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(MaybeDynamicDeviceIdBinder))] string deviceId,
        [FromBody] object value
    )
    {
        if (this.HttpContext.GetItem<IController>() is not IDirectoryController controller)
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            return this.NotFound();
        }
        return this.NotFound();
    }

    [HttpGet("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult<object>> GetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(MaybeDynamicDeviceIdBinder))] string deviceId
    )
    {
        if (this.HttpContext.GetItem<IController>() is not { } controller)
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            return this.NotFound();
        }
        if (controller is IValueController valueController)
        {
            object value = await valueController.GetValueAsync(deviceId);
            return new ValueResult(value);
        }
        if (controller is IButtonController buttonController)
        {
            await buttonController.TriggerAsync(deviceId);
            return new SuccessResult();
        }
        return this.BadRequest();
    }

    [HttpPost("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult<object>> GetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(MaybeDynamicDeviceIdBinder))] string deviceId,
        [FromBody] object value
    )
    {
        if (this.HttpContext.GetItem<IController>() is not { } controller )
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            throw new();
        }
        switch (controller)
        {
            case IDirectoryController directoryController:
                break;
            case IFavoritesController favoritesController when value is string favorite:
                await favoritesController.ExecuteAsync(deviceId, favorite);
                return new SuccessResult();
        }
        return this.BadRequest();
    }

    [HttpGet("{adapter}/{componentName}/{deviceId}/{value}")]
    public async Task<ActionResult<SuccessResult>> SetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        [ModelBinder(typeof(MaybeDynamicDeviceIdBinder))] string deviceId,
        string value
    )
    {
        if (this.HttpContext.GetItem<IController>() is not  IValueController controller )
        {
            this._logger.LogError("Can not perform action as the necessary capability or component not found.");
            return this.NotFound();
        }
        await controller.SetValueAsync(deviceId, value);
        return new SuccessResult();
    }
}