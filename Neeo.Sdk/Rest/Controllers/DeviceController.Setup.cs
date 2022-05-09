using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/discover")]
    public async Task<ActionResult<DiscoveredDevice[]>> DiscoverAsync(string adapterName)
    {
        if (await this.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}...", adapter.DeviceName);
        DiscoveredDevice[] devices = await feature.DiscoverAsync(cancellationToken: this.HttpContext.RequestAborted);
        if (devices.Length != 0 && feature.EnableDynamicDeviceBuilder)
        {
            foreach (DiscoveredDevice device in devices)
            {
                if (device.DeviceBuilder is { } builder)
                {
                    this._dynamicDevices.RegisterDiscoveredDevice(adapter, device.Id, builder);
                }
            }
        }
        return devices;
    }

    [HttpGet("{adapterName}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync(string adapterName)
    {
        if (await this.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapter.DeviceName);
        return await feature.QueryIsRegisteredAsync();
    }

    [HttpPost("{adapterName}/register")]
    public async Task<ActionResult> RegisterAsync(string adapterName, [FromBody] CredentialsPayload payload)
    {
        if (await this.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Registering {adapter}...", adapter.DeviceName);
        if (await feature.RegisterAsync(payload.Data, this._privateKey) is not { IsSuccess: false, Error: { } errorMessage })
        {
            return this.Ok();
        }
        return new ObjectResult(errorMessage)
        {
            StatusCode = (int)HttpStatusCode.Forbidden,
            ContentTypes = { "text/plain" }
        };
    }

    public readonly record struct CredentialsPayload(string Data);
}