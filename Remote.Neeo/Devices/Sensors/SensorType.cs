using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Sensors;

/// <summary>
/// An enumeration of the types of sensors available in NEEO.
/// </summary>
[JsonConverter(typeof(TextAttribute.EnumJsonConverter<SensorType>))]
public enum SensorType
{
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
