using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;

namespace Neeo.Api.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed partial class DeviceController : ControllerBase
{
    private readonly ILogger<DeviceController> _logger;
    private readonly PgpKeys _pgpKeys;

    public DeviceController(PgpKeys pgpKeys, ILogger<DeviceController> logger)
    {
        this._pgpKeys = pgpKeys;
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

    private sealed class MaybeDynamicDeviceIdBinder : IModelBinder
    {
        private readonly ILogger<MaybeDynamicDeviceIdBinder> _logger;

        public MaybeDynamicDeviceIdBinder(ILogger<MaybeDynamicDeviceIdBinder> logger) => this._logger = logger;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            string deviceId = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue!;
            if (bindingContext.HttpContext.GetItem<DynamicDevicePlaceholder>() is { ComponentName: string name })
            {
            }
            bindingContext.Result = ModelBindingResult.Success(deviceId);
            return Task.CompletedTask;



        }

        private static object? GetRouteItem(HttpContext httpContext)
        {
            return (object?)httpContext.GetItem<IController>() ?? httpContext.GetItem<DynamicDevicePlaceholder>();
        }
    }

    private sealed record DynamicDevicePlaceholder(string ComponentName);
}