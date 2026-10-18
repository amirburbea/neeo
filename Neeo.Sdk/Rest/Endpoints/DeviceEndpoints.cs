using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Neeo.Sdk.Rest.Endpoints;

internal static partial class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("device");
        DeviceEndpoints.MapRegistration(group);
        DeviceEndpoints.MapSubscriptions(group);
        DeviceEndpoints.MapDiscovery(group);
        DeviceEndpoints.MapActions(group);
        DeviceEndpoints.MapCustom(group);
    }

    /// <summary>
    /// Log category for the device endpoints (a static class cannot be used as the type argument of <c>ILogger&lt;T&gt;</c>).
    /// </summary>
    internal sealed class LogCategory;
}
