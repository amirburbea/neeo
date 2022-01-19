using System.Text.Json.Serialization;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

/// <summary>
/// The icon for a device is generally derived from the device type.
/// NEEO supports two icon overrides (specifically &quot;sonos&quot; and &quot;neeo&quot;).
/// </summary>
[JsonConverter(typeof(TextAttribute.EnumJsonConverter<DeviceIconOverride>))]
public enum DeviceIconOverride
{
    /// <summary>
    /// &quot;neeo&quot;
    /// </summary>
    [Text("neeo")]
    Neeo = 0,

    /// <summary>
    /// &quot;sonos&quot;
    /// </summary>
    [Text("sonos")]
    Sonos = 1
}