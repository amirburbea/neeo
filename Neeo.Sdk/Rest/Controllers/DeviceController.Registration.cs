using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapter.AdapterName);
        return this.Serialize(await feature.QueryIsRegisteredAsync());
    }

    [HttpPost("{adapter}/register")]
    public async Task<ActionResult<SuccessResponse>> RegisterAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, [FromBody] CredentialsPayload payload)
    {
        if (adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Registering {adapter}...", adapter.AdapterName);
        if (await feature.RegisterAsync(await PgpMethods.DeserializeEncryptedAsync<JsonElement>(payload.Data, this._pgpKeys.PrivateKey)) is not { Error: { } error })
        {
            return this.Serialize(new SuccessResponse(true));
        }
        ContentResult result = this.Content(error);
        result.StatusCode = 500;
        return result;
    }

    public record struct CredentialsPayload(string Data);
}