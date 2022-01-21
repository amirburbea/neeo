using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class DeviceController : ControllerBase
{
    [HttpGet("/{adapter}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync(
        [ModelBinder(typeof(DeviceAdapterBinder))] IDeviceAdapter adapter
    ) => adapter.GetCapabilityHandler(ComponentType.Registration) is not { Controller: IRegistrationController controller }
            ? throw new NotSupportedException(nameof(this.QueryIsRegisteredAsync))
            : await controller.QueryIsRegisteredAsync();

    [HttpPost("/{adapter}/register")]
    public async Task<ActionResult<SuccessResult>> RegisterAsync(
        [ModelBinder(typeof(DeviceAdapterBinder))] IDeviceAdapter adapter,
        [FromBody] JsonElement credentials
    ) => adapter.GetCapabilityHandler(ComponentType.Registration) is not { Controller: IRegistrationController controller }
            ? throw new NotSupportedException(nameof(this.RegisterAsync))
            : await controller.RegisterAsync(credentials);

    private sealed class DeviceAdapterBinder : IModelBinder
    {
        private readonly IDeviceDatabase _database;

        public DeviceAdapterBinder(IDeviceDatabase database) => this._database = database;

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue is not string adapterName)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }
            bindingContext.Result = ModelBindingResult.Success(
                bindingContext.HttpContext.Items[typeof(IDeviceAdapter)] = await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false)
            );
        }
    }

}