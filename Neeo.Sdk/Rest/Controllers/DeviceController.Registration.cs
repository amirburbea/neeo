using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/registered")]
    public async Task<ActionResult> QueryIsRegisteredAsync(string adapterName)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapterName);
        return this.Ok(new { Registered = await feature.QueryIsRegisteredAsync() });
    }

    [HttpPost("{adapterName}/register")]
    public async Task<ActionResult> RegisterAsync(string adapterName, [FromBody] CredentialsPayload payload)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Registering {adapter}...", adapterName);
        using Stream stream = this.DeserializeEncrypted(payload.Data);
        return await feature.RegisterAsync(stream) is not { Error: { } error }
            ? this.Ok()
            : this.StatusCode((int)HttpStatusCode.InternalServerError, error);
    }

    private Stream DeserializeEncrypted(string armoredText)
    {
        const string invalidTextError = "Invalid input text.";
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(armoredText));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject next = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject();
        if (next is not PgpEncryptedDataList { Count: > 0 } list || list[0] is not PgpPublicKeyEncryptedData data)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream privateStream = data.GetDataStream(this._privateKey);
        PgpObjectFactory privateFactory = new(privateStream);
        if (privateFactory.NextPgpObject() is not PgpLiteralData literal)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        return literal.GetInputStream();
    }

    public record struct CredentialsPayload(string Data);
}