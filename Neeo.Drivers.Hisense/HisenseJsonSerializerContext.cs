using System.Text.Json.Serialization;

namespace Neeo.Drivers.Hisense;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(AppInfo))]
[JsonSerializable(typeof(AppInfo[]))]
[JsonSerializable(typeof(SourceInfo))]
[JsonSerializable(typeof(SourceInfo[]))]
[JsonSerializable(typeof(HisenseTV.SuccessPayload))]
[JsonSerializable(typeof(HisenseTV.AuthPayload))]
[JsonSerializable(typeof(HisenseTV.VolumeData))]
[JsonSerializable(typeof(HisenseTV.StateData))]
internal sealed partial class HisenseJsonSerializerContext : JsonSerializerContext;
