using System.ComponentModel;
using System.Text.Json.Serialization;
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

    
    [Text("power")]
    Power,

    [Text("range")]
    Range,

    [Text("string")]
    String,


    /// <summary>
    /// Not recommended for use.
    /// </summary>
    [Text("custom"), EditorBrowsable(EditorBrowsableState.Never)]
    Custom,
}
