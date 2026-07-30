using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Rest.Controllers;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Source-generated JSON metadata for the types that flow across the wire to/from the NEEO Brain.
/// Avoids the reflection/expression-tree-emit path of the default serializer, which is
/// disproportionately expensive on low-end ARM hardware (e.g. Raspberry Pi 2/3) without
/// vectorized/AVX-style instructions. See <see cref="AppJsonSerializerOptions"/> for how this is
/// combined with a reflection fallback for the SDK's open-generic call sites.
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
[JsonSerializable(typeof(NotificationMapping.Entry[]))]
[JsonSerializable(typeof(BrainRecipes.RecipeDefinition[]))]
[JsonSerializable(typeof(BrainRecipes.Recipe.EmptyObject))]
[JsonSerializable(typeof(DeviceController.FavoritePayload))]
[JsonSerializable(typeof(DeviceController.DirectoryActionPayload))]
[JsonSerializable(typeof(DeviceController.CredentialsPayload))]
[JsonSerializable(typeof(DeviceController.DynamicDiscoveredDevice))]
[JsonSerializable(typeof(SecureController.PgpPublicKeyResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(SdkRegistration.RegisterServerRequest))]
[JsonSerializable(typeof(SdkRegistration.UnregisterServerRequest))]
[JsonSerializable(typeof(DeviceBuilder.Credentials))]
[JsonSerializable(typeof(DeviceBuilder.SecurityCodeContainer))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
