using System.Text.Json.Serialization;

namespace Neeo.Drivers.WebOS.Models;

public readonly record struct WebOSRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("uri"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Uri = default,
    [property: JsonPropertyName("payload"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Payload = default
);
