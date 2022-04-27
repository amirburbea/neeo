using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/discover")]
    public async Task<ActionResult<DiscoveredDevice[]>> DiscoverAsync(string adapterName)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}.", adapter.AdapterName);
        DiscoveredDevice[] devices = await feature.DiscoverAsync(cancellationToken: this.HttpContext.RequestAborted);
        if (feature.EnableDynamicDeviceBuilder)
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
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapterName);
        return new IsRegisteredResponse(await feature.QueryIsRegisteredAsync());
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
        if (await feature.RegisterAsync(stream) is not { Error: { } error })
        {
            return this.Ok();
        }
        return new ObjectResult(error)
        { 
            StatusCode = (int)HttpStatusCode.Forbidden,
            ContentTypes = { "text/plain" }
        };
    }

    private Stream DeserializeEncrypted(string armoredText)
    {
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(armoredText));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject next = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject();
        const string invalidTextError = "Invalid input text.";
        if (next is not PgpEncryptedDataList { Count: > 0 } list || list[0] is not PgpPublicKeyEncryptedData data)
        {
            throw new ArgumentException(invalidTextError, nameof(armoredText));
        }
        using Stream privateStream = data.GetDataStream(this._privateKey);
        PgpObjectFactory privateFactory = new(privateStream);
        if (privateFactory.NextPgpObject() is not PgpLiteralData literal)
        {
            throw new ArgumentException(invalidTextError, nameof(armoredText));
        }
        return literal.GetInputStream();
    }

    public readonly record struct CredentialsPayload(string Data);

    public readonly record struct IsRegisteredResponse(bool Registered);
}