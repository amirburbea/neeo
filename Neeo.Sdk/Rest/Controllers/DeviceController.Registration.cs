using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature controller)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapter.AdapterName);
        return this.Ok(await controller.QueryIsRegisteredAsync());
    }

    [HttpPost("{adapter}/register")]
    public async Task<ActionResult<SuccessResponse>> RegisterAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, [FromBody] CredentialsPayload payload)
    {
        if (adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature controller)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Registering {adapter}...", adapter.AdapterName);
        if (await controller.RegisterAsync(await this._pgp.DeserializeEncryptedAsync<System.Text.Json.JsonElement>(payload.Data)) is not { Error: { } error })
        {
            return this.Ok(new SuccessResponse());
        }
        ContentResult result = this.Content(error);
        result.StatusCode = 500;
        return result;
    }

    public record struct CredentialsPayload(string Data);

    public record struct IsRegisteredResponse(bool Registered);
}