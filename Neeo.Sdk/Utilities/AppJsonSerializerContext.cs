using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Rest.Endpoints;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Source-generated JSON metadata for the types that flow across the wire to/from the NEEO Brain.
/// Avoids the reflection/expression-tree-emit path of the default serializer, which is
/// disproportionately expensive on low-end ARM hardware (e.g. Raspberry Pi 2/3) without
/// vectorized/AVX-style instructions, and is not available at all under AOT. Every type that is
/// serialized by the SDK must be registered here; use <see cref="TypeInfo{T}"/> to resolve metadata
/// for the SDK's generic call sites.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(Component))]
[JsonSerializable(typeof(SensorDetails))]
[JsonSerializable(typeof(SliderDetails))]
[JsonSerializable(typeof(IDirectoryItem))]
[JsonSerializable(typeof(DirectoryButtonData))]
[JsonSerializable(typeof(DirectoryTileData))]
[JsonSerializable(typeof(DirectoryData))]
[JsonSerializable(typeof(BrowseParameters))]
[JsonSerializable(typeof(DeviceModel))]
[JsonSerializable(typeof(DeviceSearchResult[]))]
[JsonSerializable(typeof(SuccessResponse))]
[JsonSerializable(typeof(IsRegisteredResponse))]
[JsonSerializable(typeof(ValueResponse))]
[JsonSerializable(typeof(DiscoveredDevice[]))]
[JsonSerializable(typeof(NotificationService.Message))]
[JsonSerializable(typeof(NotificationService.Message.SensorData))]
// Sensor and value payloads are typed as `object`, so the runtime types a driver can supply must be registered.
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(NotificationMapping.Entry[]))]
[JsonSerializable(typeof(BrainRecipes.RecipeDefinition[]))]
[JsonSerializable(typeof(BrainRecipes.Recipe.EmptyObject))]
[JsonSerializable(typeof(DeviceEndpoints.FavoritePayload))]
[JsonSerializable(typeof(DeviceEndpoints.DirectoryActionPayload))]
[JsonSerializable(typeof(DeviceEndpoints.CredentialsPayload))]
[JsonSerializable(typeof(DeviceEndpoints.DynamicDiscoveredDevice))]
[JsonSerializable(typeof(DeviceEndpoints.DynamicDiscoveredDevice[]))]
[JsonSerializable(typeof(SecureEndpoints.PgpPublicKeyResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(SdkRegistration.RegisterServerRequest))]
[JsonSerializable(typeof(SdkRegistration.UnregisterServerRequest))]
[JsonSerializable(typeof(DeviceBuilder.Credentials))]
[JsonSerializable(typeof(DeviceBuilder.SecurityCodeContainer))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
