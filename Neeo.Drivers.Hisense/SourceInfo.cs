using System.Text.Json.Serialization;

namespace Neeo.Drivers.Hisense;

public record struct SourceInfo([property: JsonPropertyName("sourcename")] string Name, [property: JsonPropertyName("sourceid")] int SourceId, [property: JsonPropertyName("displayname")] string DisplayName);