using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public readonly record struct LibrarySectionInfo(
    int Key,
    string Title,
    LibrarySectionType Type,
    [property: JsonPropertyName("thumb")] string? Thumbnail = default
);

[JsonConverter(typeof(TextJsonConverter<LibrarySectionType>))]
public enum LibrarySectionType
{
    [Text("movie")]
    Movie,

    [Text("show")]
    Show,

    [Text("artist")]
    Artist,

    [Text("photo")]
    Photo,
}
