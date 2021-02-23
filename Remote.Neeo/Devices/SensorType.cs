using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// An enumeration of the types of sensors available in NEEO.
    /// </summary>
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<SensorType>))]
    public enum SensorType
    {
        [Text("array")]
        Array = 0,

        [Text("binary")]
        Binary,

        [Text("custom")]
        Custom,

        [Text("power")]
        Power,

        [Text("range")]
        Range,

        [Text("switch")]
        String,
    }
}