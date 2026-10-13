using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Drivers.WebOS.Models;

namespace Neeo.Drivers.WebOS;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(WebOSRequest))]
[JsonSerializable(typeof(WebOSResponse))]
[JsonSerializable(typeof(WebOSAppInfo))]
[JsonSerializable(typeof(WebOSAppInfo[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonElement?))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
internal sealed partial class WebOSJsonSerializerContext : JsonSerializerContext;
