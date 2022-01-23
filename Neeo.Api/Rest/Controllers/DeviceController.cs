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
    private readonly PgpComponents _pgpComponents;

    public DeviceController(PgpComponents pgp, ILogger<DeviceController> logger)
    {
        this._pgpComponents = pgp;
        this._logger = logger;
    }

    private sealed class AdapterBinder : IModelBinder
    {
        private readonly IDeviceDatabase _database;
        private readonly ILogger<AdapterBinder> _logger;

        public AdapterBinder(IDeviceDatabase database, ILogger<AdapterBinder> logger) => (this._database, this._logger) = (database, logger);

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue is not { } adapterName)
            {
                this._logger.LogWarning("Failed to resolve adapter. No adapter name.");
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Check route parameter name for adapter.");
                return;
            }
            IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false);
            bindingContext.HttpContext.SetItem(adapter);
            bindingContext.Result = ModelBindingResult.Success(adapter);
        }
    }

    private sealed class ComponentNameBinder : IModelBinder
    {
        private readonly ILogger<ComponentNameBinder> _logger;

        public ComponentNameBinder(ILogger<ComponentNameBinder> logger) => this._logger = logger;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue is not { } componentName)
            {
                this._logger.LogWarning("Failed to resolve component. No component name.");
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Check route parameter name for component.");
                return Task.CompletedTask;
            }
            if (bindingContext.HttpContext.GetItem<IDeviceAdapter>() is not IDeviceAdapter adapter)
            {
                this._logger.LogWarning("Failed to resolve component. No adapter.");
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "No adapter.");
                return Task.CompletedTask;
            }
            if (adapter.GetCapabilityHandler(componentName) is { } handler)
            {
                bindingContext.HttpContext.SetItem(handler);
            }
            else
            {
                bindingContext.HttpContext.SetItem(new DynamicDevicePlaceholder(componentName));
            }
            bindingContext.Result = ModelBindingResult.Success(componentName);
            return Task.CompletedTask;
        }
    }

    private sealed class MaybeDynamicDeviceIdBinder
    {

    }
    
    
    private record struct DynamicDevicePlaceholder(string ComponentName);
}