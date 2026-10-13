using System.Text.Json.Serialization;

namespace Neeo.Drivers.WebOS.Models;

public readonly record struct WebOSAppInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("icon")] string? Icon = default,
    [property: JsonPropertyName("largeIcon")] string? LargeIcon = default
);
