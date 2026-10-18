using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest.Endpoints;

internal static partial class DeviceEndpoints
{
    private static void MapCustom(RouteGroupBuilder group)
    {
        group.Map("{adapterName}/custom/{**suffix}", DeviceEndpoints.HandleCustomRouteAsync);
    }

    private static async Task<IResult> HandleCustomRouteAsync(
        string adapterName,
        string? suffix,
        HttpRequest request,
        IDeviceDatabase database,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.RouteHandler is not { } handler)
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Running route handler for {adapter} with '{suffix}'", adapter.DeviceName, suffix);
        return await handler(request, suffix ?? string.Empty, cancellationToken);
    }
}
