using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// The icon for a device is generally derived from the device type.
    /// NEEO supports two icon overrides (specifically &quot;sonos&quot; and &quot;neeo&quot;).
    /// </summary>
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<DeviceIconOverride>))]
    public enum DeviceIconOverride
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
