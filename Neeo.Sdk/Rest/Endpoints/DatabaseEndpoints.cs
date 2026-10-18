using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest.Endpoints;

internal static class DatabaseEndpoints
{
    public static void MapDatabaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("db");
        group.MapGet("adapterdefinition/{adapterName}", DatabaseEndpoints.GetDeviceByAdapterName);
        group.MapGet("{deviceId}", DatabaseEndpoints.GetDeviceById);
        group.MapGet("search", DatabaseEndpoints.Search);
    }

    private static Results<Ok<DeviceModel>, NotFound> GetDeviceByAdapterName(string adapterName, IDeviceDatabase database)
    {
        return database.GetDeviceByAdapterName(adapterName) is { } device ? TypedResults.Ok(device) : TypedResults.NotFound();
    }

    private static Results<Ok<DeviceModel>, NotFound> GetDeviceById(int deviceId, IDeviceDatabase database)
    {
        return database.GetDeviceById(deviceId) is { } device ? TypedResults.Ok(device) : TypedResults.NotFound();
    }

    private static DeviceSearchResult[] Search([FromQuery(Name = "q")] string? query, IDeviceDatabase database)
    {
        return database.Search(query);
    }
}
