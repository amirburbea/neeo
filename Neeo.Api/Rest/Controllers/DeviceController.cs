using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices;

namespace Neeo.Api.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed partial class DeviceController : ControllerBase
{
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(ILogger<DeviceController> logger) => this._logger = logger;

    private sealed class AdapterBinder : IModelBinder
    {
        private readonly IDeviceDatabase _database;
        private ILogger<AdapterBinder> _logger;

        public AdapterBinder(IDeviceDatabase database, ILogger<AdapterBinder> logger) => (this._database, this._logger) = (database, logger);

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue is not { } adapterName)
            {
                bindingContext.ModelState.AddModelError(nameof(IDeviceAdapter), "Check route parameter name.");
                return;
            }
            IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false);
            bindingContext.HttpContext.Items[nameof(IDeviceAdapter)] = adapter;
            bindingContext.Result = ModelBindingResult.Success(adapter);
        }
    }
}