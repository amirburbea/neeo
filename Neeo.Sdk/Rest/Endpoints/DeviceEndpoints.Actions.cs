using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest.Endpoints;

internal static partial class DeviceEndpoints
{
    public readonly record struct DirectoryActionPayload(string ActionIdentifier);

    public readonly record struct FavoritePayload(string FavoriteId);

    private static void MapActions(RouteGroupBuilder group)
    {
        group.MapPost("{adapterName}/{componentName}/{deviceId}", DeviceEndpoints.ExecuteAsync);
        group.MapGet("{adapterName}/{componentName}/{deviceId}", DeviceEndpoints.GetValueAsync);
        group.MapPost("{adapterName}/{componentName}/{deviceId}/action", DeviceEndpoints.PerformDirectoryActionAsync);
        group.MapGet("{adapterName}/{componentName}/{deviceId}/{value}", DeviceEndpoints.SetValueAsync);
    }

    private static async Task<Results<Ok<SuccessResponse>, Ok<DirectoryData>, NotFound>> ExecuteAsync(
        string adapterName,
        string componentName,
        string deviceId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement parameters,
        IDeviceDatabase database,
        IDynamicDeviceRegistry dynamicDeviceRegistry,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await DeviceEndpoints.TryResolveAsync(database, dynamicDeviceRegistry, adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, { } feature))
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Execute {Type}:{Component} on {Name}:{Id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IFavoritesFeature favoritesFeature => TypedResults.Ok(await favoritesFeature.ExecuteAsync(
                deviceId,
                parameters.Deserialize(AppJsonSerializerContext.Default.FavoritePayload).FavoriteId,
                cancellationToken
            )),
            IDirectoryFeature directoryFeature => TypedResults.Ok(await directoryFeature.BrowseAsync(
                deviceId,
                parameters.Deserialize(AppJsonSerializerContext.Default.BrowseParameters),
                cancellationToken
            )),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<Ok<SuccessResponse>, Ok<ValueResponse>, NotFound>> GetValueAsync(
        string adapterName,
        string componentName,
        string deviceId,
        IDeviceDatabase database,
        IDynamicDeviceRegistry dynamicDeviceRegistry,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await DeviceEndpoints.TryResolveAsync(database, dynamicDeviceRegistry, adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, { } feature))
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Get {Type}:{Component} on {Name}:{Id}", feature.Type, componentName, adapter.DeviceName, deviceId);
        return feature switch
        {
            IButtonFeature buttonFeature => TypedResults.Ok(await buttonFeature.ExecuteAsync(deviceId, cancellationToken)),
            IValueFeature valueFeature => TypedResults.Ok(await valueFeature.GetValueAsync(deviceId, cancellationToken)),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<Ok, NotFound>> PerformDirectoryActionAsync(
        string adapterName,
        string componentName,
        string deviceId,
        [FromBody] DirectoryActionPayload action,
        IDeviceDatabase database,
        IDynamicDeviceRegistry dynamicDeviceRegistry,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await DeviceEndpoints.TryResolveAsync(database, dynamicDeviceRegistry, adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, IDirectoryFeature directoryFeature))
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Perform directory action {Action} on {Name}:{Id}", action.ActionIdentifier, adapter.DeviceName, deviceId);
        await directoryFeature.PerformActionAsync(deviceId, action.ActionIdentifier, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<SuccessResponse>, NotFound>> SetValueAsync(
        string adapterName,
        string componentName,
        string deviceId,
        string value,
        IDeviceDatabase database,
        IDynamicDeviceRegistry dynamicDeviceRegistry,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await DeviceEndpoints.TryResolveAsync(database, dynamicDeviceRegistry, adapterName, componentName, deviceId, cancellationToken) is not ({ } adapter, IValueFeature valueFeature))
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Set {Component} value to {Value} on {Name}:{Id}", componentName, value, adapter.DeviceName, deviceId);
        return TypedResults.Ok(await valueFeature.SetValueAsync(deviceId, value, cancellationToken));
    }

    private static async ValueTask<(IDeviceAdapter, IFeature)> TryResolveAsync(
        IDeviceDatabase database,
        IDynamicDeviceRegistry dynamicDeviceRegistry,
        string adapterName,
        string componentName,
        string deviceId,
        CancellationToken cancellationToken
    )
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken).ConfigureAwait(false) is not { } adapter)
        {
            return default;
        }
        // Static device component.
        if (adapter.GetFeature(componentName) is { } feature)
        {
            return (adapter, feature);
        }
        // Check for a discovered device with the name `deviceId` and has a component named `componentName`.
        if (await dynamicDeviceRegistry.GetDiscoveredDeviceAsync(adapter, deviceId, cancellationToken).ConfigureAwait(false) is { } dynamicAdapter && dynamicAdapter.GetFeature(componentName) is { } dynamicFeature)
        {
            return (dynamicAdapter, dynamicFeature);
        }
        return default;
    }
}
