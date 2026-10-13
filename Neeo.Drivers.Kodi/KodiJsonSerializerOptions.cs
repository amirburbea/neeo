using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Neeo.Drivers.Kodi;

/// <summary>
/// The canonical <see cref="JsonSerializerOptions"/> instance used across the Kodi driver, combining
/// <see cref="KodiJsonSerializerContext"/> (avoids reflection/emit for known Kodi wire types) with a
/// reflection-based fallback resolver (<see cref="DefaultJsonTypeInfoResolver"/>) for anonymous types
/// and dynamic RPC calls.
/// </summary>
internal static class KodiJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new(KodiJsonSerializerContext.Default.Options)
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(
            KodiJsonSerializerContext.Default,
            new DefaultJsonTypeInfoResolver()
        ),
    };
}
