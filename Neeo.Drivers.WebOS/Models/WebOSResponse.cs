using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.WebOS.Models;

public readonly record struct WebOSResponse(
    [property: JsonPropertyName("id")] string? Id = default,
    [property: JsonPropertyName("type")] string? Type = default,
    [property: JsonPropertyName("payload")] JsonElement Payload = default,
    [property: JsonPropertyName("error")] string? Error = default
);
