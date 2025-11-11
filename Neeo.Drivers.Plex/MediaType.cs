using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

[JsonConverter(typeof(TextJsonConverter<MediaType>))]
public enum MediaType
{
    [Text("episode")]
    Episode,

    [Text("music")]
    Music,

    [Text("photo")]
    Photo,

    [Text("video")]
    Video,
}
