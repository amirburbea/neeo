using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Device icons.
    /// </summary>
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<DeviceIcon>))]
    public enum DeviceIcon
    {
        /// <summary>
        /// Neeo.
        /// </summary>
        [Text("neeo")]
        Neeo = 0,

        /// <summary>
        /// Sonos.
        /// </summary>
        [Text("sonos")]
        Sonos = 1
    }
}
