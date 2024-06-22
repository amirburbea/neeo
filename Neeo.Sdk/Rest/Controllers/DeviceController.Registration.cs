using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;

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
        logger.LogInformation("Querying registration for {adapter}...", adapter.DeviceName);
        return await feature.QueryIsRegisteredAsync(cancellationToken);
    }

    [HttpPost("{adapterName}/register")]
    public async Task<ActionResult> RegisterAsync(
        string adapterName,
        [FromBody] CredentialsPayload payload,
        CancellationToken cancellationToken
    )
    {
        if (await this.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return this.NotFound();
        }
        logger.LogInformation("Registering {adapter}...", adapter.DeviceName);
        return await feature.RegisterAsync(payload.Data, pgpKeys.PrivateKey, cancellationToken) is { IsSuccess: false, Error: { } text }
            ? new ObjectResult(text) { StatusCode = (int)HttpStatusCode.Forbidden, ContentTypes = { "text/plain" } }
            : this.Ok();
    }

    public readonly record struct CredentialsPayload(string Data);
}
