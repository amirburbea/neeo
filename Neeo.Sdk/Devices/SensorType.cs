using System.ComponentModel;
using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An enumeration of the types of sensors available in NEEO.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<SensorType>))]
public enum SensorType
{
    /// <summary>
    /// Binary (boolean) sensor type.
    /// </summary>
    [Text("binary")]
    Binary,

    /// <summary>
    /// Power state sensor type.
    /// </summary>
    [Text("power")]
    Power,

    /// <summary>
    /// Numeric range sensor type.
    /// </summary>
    [Text("range")]
    Range,

    /// <summary>
    /// String (text) sensor type.
    /// </summary>
    [Text("string")]
    String,

    /// <summary>
    /// Not recommended for use.
    /// </summary>
    [Text("custom")]
    Custom
}