using System.Text.Json.Serialization;

namespace Neeo.Api.Devices;

/// <summary>
/// An enumeration of the types of sensors available in NEEO.
/// </summary>
[JsonConverter(typeof(TextAttribute.EnumJsonConverter<SensorTypes>))]
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
