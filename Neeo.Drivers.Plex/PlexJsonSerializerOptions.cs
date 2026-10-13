using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Neeo.Drivers.Plex;

/// <summary>
/// The canonical <see cref="JsonSerializerOptions"/> instance used across the Plex driver, combining
/// <see cref="PlexJsonSerializerContext"/> (avoids reflection/emit for known Plex wire types) with a
/// reflection-based fallback resolver (<see cref="DefaultJsonTypeInfoResolver"/>).
/// </summary>
internal static class PlexJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new(PlexJsonSerializerContext.Default.Options)
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(
            PlexJsonSerializerContext.Default,
            new DefaultJsonTypeInfoResolver()
        ),
    };
}
