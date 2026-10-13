using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

[JsonConverter(typeof(TextJsonConverter<PlayState>))]
public enum PlayState
{
    [Text("stopped")]
    Stopped = 0,

    [Text("playing")]
    Playing = 1,

    [Text("paused")]
    Paused = 2,

    [Text("buffering")]
    Buffering = 3,

    [Text("error")]
    Error = 4,
}
