﻿using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An enumeration of the types of sensors available in NEEO.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<SensorTypes>))]
public enum SensorTypes
{
    /// <summary>
    /// A boolean switch.
    /// </summary>
    [Text("binary")]
    Binary,

    [Text("custom")]
    Custom,

    [Text("power")]
    Power,

    [Text("range")]
    Range,

    [Text("string")]
    String,
}