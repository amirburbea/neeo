using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync(string adapterName, CancellationToken cancellationToken)
    {
        if (await this.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        logger.LogInformation("Querying registration for {Adapter}...", adapter.DeviceName);
        return await feature.QueryIsRegisteredAsync(cancellationToken);
    }

    [HttpPost("{adapterName}/register")]
    public async Task<ActionResult> RegisterAsync(string adapterName, [FromBody] CredentialsPayload payload, CancellationToken cancellationToken)
    {
        if (await this.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        // Extract the credentials as JSON bytes.
        if (await pgpEncryption.DecryptAsync(payload.Data, cancellationToken) is not { } decryptedJsonBytes)
        {
            return this.BadRequest();
        }
        logger.LogInformation("Registering {Adapter}...", adapter.DeviceName);
        if (await feature.RegisterAsync(decryptedJsonBytes, cancellationToken) is { IsSuccess: false, Error: { } text })
        {
            return new ObjectResult(text) { StatusCode = (int)HttpStatusCode.Forbidden, ContentTypes = { "text/plain" } };
        }
        return this.Ok();
    }

    public readonly record struct CredentialsPayload(string Data);
}
