using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Json;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Registration) is not { Controller: IRegistrationController controller })
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapter.AdapterName);
        return new IsRegisteredResponse(await controller.QueryIsRegisteredAsync());
    }

    [HttpPost("{adapter}/register")]
    public async Task<ActionResult<SuccessResult>> RegisterAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, [FromBody] CredentialsPayload payload)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Registration) is not { Controller: IRegistrationController controller })
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Registering {adapter}...", adapter.AdapterName);
        await controller.RegisterAsync(await DecryptElementAsync(payload.Data));
        return new SuccessResult();
    }

    private async ValueTask<JsonElement> DecryptElementAsync(string data)
    {
        using Stream stream = this._pgpComponents.Decrypt(data);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, JsonSerialization.Options);
    }

    public record struct CredentialsPayload(string Data);

    public record struct IsRegisteredResponse(bool Registered);
}