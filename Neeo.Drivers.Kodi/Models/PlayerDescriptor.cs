﻿using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public readonly record struct PlayerDescriptor(
    string Name,
    [property: JsonPropertyName("playsaudio")] bool PlaysAudio,
    [property: JsonPropertyName("playsvideo")] bool PlaysVideo,
    string Type
);
