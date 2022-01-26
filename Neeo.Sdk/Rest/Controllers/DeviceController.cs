using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed partial class DeviceController : ControllerBase
{
    private readonly ILogger<DeviceController> _logger;
    private readonly PgpKeys _pgpKeys;

    public DeviceController(
        PgpKeys pgpKeys,
        ILogger<DeviceController> logger
    ) => (this._pgpKeys, this._logger) = (pgpKeys, logger);

    private sealed class AdapterBinder : ParameterBinderBase<IDeviceAdapter>
    {
        private readonly IDeviceDatabase _database;

        public AdapterBinder(IDeviceDatabase database, ILogger<AdapterBinder> logger)
            : base(logger) => this._database = database;

        protected override async Task<Result> Resolve(string adapterName, HttpContext httpContext)
        {
            IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false);
            // Adapter was resolved. Set the adapter in the request.
            httpContext.SetItem(adapter);
            return Result.Success(adapter);
        }
    }

    private sealed class ComponentNameBinder : ParameterBinderBase<string>
    {
        private readonly IDynamicDevices _dynamicDevices;

        public ComponentNameBinder(IDynamicDevices dynamicDevices, ILogger<ComponentNameBinder> logger)
            : base(logger, nameof(IController))
        {
            this._dynamicDevices = dynamicDevices;
        }

        protected override Task<Result> Resolve(string componentName, HttpContext httpContext)
        {
            if (httpContext.GetItem<IDeviceAdapter>() is not { } adapter)
            {
                return Task.FromResult(Result.Failed("No adapter."));
            }
            if (adapter.GetCapabilityHandler(componentName) is { } controller)
            {
                // Static device - set the controller in the request.
                httpContext.SetItem(controller);
            }
            else
            {
                this.Logger.LogInformation("Dynamic device needed for {component}.", componentName);
                this._dynamicDevices.StorePlaceholderInRequest(httpContext, adapter, componentName);
            }
            return Task.FromResult(Result.Success(componentName));
        }
    }

    private sealed class DeviceIdBinder : ParameterBinderBase<string>
    {
        private readonly IDynamicDevices _dynamicDevices;

        public DeviceIdBinder(IDynamicDevices dynamicDevices, ILogger<DeviceIdBinder> logger)
            : base(logger, "deviceId") => this._dynamicDevices = dynamicDevices;

        protected override async Task<Result> Resolve(string deviceId, HttpContext httpContext)
        {
            if (!httpContext.HasItem<IDeviceAdapter>())
            {
                return Result.Failed("No adapter.");
            }
            if (httpContext.GetItem<IController>() != null)
            {
                // Static device.
                return Result.Success(deviceId);
            }
            if (!this._dynamicDevices.HasPlaceholder(httpContext))
            {
                return Result.Failed("No dynamic device placeholder.");
            }
            if (await this._dynamicDevices.StoreDiscoveryHandlerInRequestAsync(httpContext, deviceId).ConfigureAwait(false))
            {
                // Dynamic device was resolved and controller is now in the request.
                return Result.Success(deviceId);
            }
            return Result.Failed($"Device not found for {deviceId}.");
        }
    }

    private abstract class ParameterBinderBase<T> : IModelBinder
        where T : notnull
    {
        private readonly string _name;

        protected ParameterBinderBase(ILogger logger, string? name = default)
        {
            this.Logger = logger;
            this._name = name ?? typeof(T).Name;
        }

        protected ILogger Logger { get; }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            string? error;
            if (bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue is not { } text)
            {
                error = "Invalid route mapping.";
            }
            else
            {
                (T? value, error) = await this.Resolve(text, bindingContext.HttpContext).ConfigureAwait(false);
                if (value is not null)
                {
                    bindingContext.Result = ModelBindingResult.Success(value);
                    return;
                }
            }
            this.Logger.LogWarning("Failed to bind {name}. ({error})", this._name, error);
            bindingContext.Result = ModelBindingResult.Failed();
        }

        protected record struct Result(T? Value = default, string? Error = default)
        {
            public static Result Success(T value) => new(value);

            public static Result Failed(string error) => new(Error: error);
        }

        protected abstract Task<Result> Resolve(string text, HttpContext httpContext);
    }
}