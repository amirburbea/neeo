using System;
using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

[Flags, JsonConverter(typeof(TextJsonConverter<PlayerCapabilities>))]
public enum PlayerCapabilities
{
    None = 0,

    [Text("playback")]
    Playback = 1,

    [Text("navigation")]
    Navigation = 2,

    [Text("timeline")]
    Timeline = 4,

    [Text("playqueues")]
    Playqueues = 8,

    [Text("provider-playback")]
    ProviderPlayback = 16,

    [Text("mirror")]
    Mirror = 32,
}