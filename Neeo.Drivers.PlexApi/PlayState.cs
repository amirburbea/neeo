using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

[JsonConverter(typeof(TextJsonConverter<PlayState>))]
public enum PlayState
{
    [Text("playing")]
    Playing,

    [Text("paused")]
    Paused,

    [Text("stopped")]
    Stopped,

    [Text("buffering")]
    Buffering,

    [Text("error")]
    Error
}
