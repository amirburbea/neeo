using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct TimelineEntry(
    string Identifier,
    [property: JsonPropertyName("itemID")] int ItemId,
    string MetadataState,
    [property: JsonPropertyName("sectionID")] int SectionId,
    int State,
    int Type,
    long UpdatedAt,
    string? Title
);
