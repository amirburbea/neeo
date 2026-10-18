using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Endpoints;

internal static partial class DeviceEndpoints
{
    public readonly record struct CredentialsPayload(string Data);

    private static void MapRegistration(RouteGroupBuilder group)
    {
        group.MapGet("{adapterName}/registered", DeviceEndpoints.QueryIsRegisteredAsync);
        group.MapPost("{adapterName}/register", DeviceEndpoints.RegisterAsync);
    }

    private static async Task<Results<Ok<IsRegisteredResponse>, NotFound>> QueryIsRegisteredAsync(
        string adapterName,
        IDeviceDatabase database,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Querying registration for {Adapter}...", adapter.DeviceName);
        return TypedResults.Ok(await feature.QueryIsRegisteredAsync(cancellationToken));
    }

    private static async Task<IResult> RegisterAsync(
        string adapterName,
        [FromBody] CredentialsPayload payload,
        IDeviceDatabase database,
        IPgpEncryption pgpEncryption,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.GetFeature(ComponentType.Registration) is not IRegistrationFeature feature)
        {
            return TypedResults.NotFound();
        }
        // Extract the credentials as JSON bytes.
        if (payload.Data is null || await pgpEncryption.DecryptAsync(payload.Data, cancellationToken) is not { } decryptedJsonBytes)
        {
            return TypedResults.BadRequest();
        }
        logger.LogInformation("Registering {Adapter}...", adapter.DeviceName);
        if (await feature.RegisterAsync(decryptedJsonBytes, cancellationToken) is { IsSuccess: false, Error: { } text })
        {
            return Results.Text(text, "text/plain", Encoding.UTF8, StatusCodes.Status403Forbidden);
        }
        return TypedResults.Ok();
    }
}
