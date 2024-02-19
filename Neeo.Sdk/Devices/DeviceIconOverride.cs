using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// The icon for a device is generally derived from the device type.
/// NEEO supports two icon overrides (specifically &quot;sonos&quot; and &quot;neeo&quot;).
/// </summary>
[JsonConverter(typeof(TextJsonConverter<DeviceIconOverride>))]
public enum DeviceIconOverride
{
    /// <summary>
    /// &quot;neeo&quot;
    /// </summary>
    [Text("neeo-brain")]
    NeeoBrain = 0,

    /// <summary>
    /// &quot;sonos&quot;
    /// </summary>
    [Text("sonos")]
    Sonos = 1
}
