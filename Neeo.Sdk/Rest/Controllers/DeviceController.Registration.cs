using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;
using Neeo.Sdk.Json;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Registration) is not IRegistrationController controller)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapter.AdapterName);
        return new IsRegisteredResponse(await controller.QueryIsRegisteredAsync());
    }

    [HttpPost("{adapter}/register")]
    public async Task<ActionResult<SuccessResult>> RegisterAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, [FromBody] CredentialsPayload payload)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Registration) is not IRegistrationController controller)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Registering {adapter}...", adapter.AdapterName);
        await controller.RegisterAsync(await this.DecryptElementAsync(payload.Data));
        return new SuccessResult();
    }

    private async ValueTask<JsonElement> DecryptElementAsync(string text)
    {
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(text));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject root = inputFactory.NextPgpObject();
        const string invalidTextError = "Text was not in the expected format.";
        if ((root as PgpEncryptedDataList ?? inputFactory.NextPgpObject()) is not PgpEncryptedDataList list || list[0] is not PgpPublicKeyEncryptedData data)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream privateStream = data.GetDataStream(this._pgpKeys.PrivateKey);
        PgpObjectFactory privateFactory = new(privateStream);
        if (privateFactory.NextPgpObject() is not PgpLiteralData literal)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream stream = literal.GetInputStream();
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, JsonSerialization.Options);
    }

    public record struct CredentialsPayload(string Data);

    public record struct IsRegisteredResponse(bool Registered);
}