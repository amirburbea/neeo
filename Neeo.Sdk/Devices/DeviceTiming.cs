using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A structure for specifying the delays NEEO should use when interacting with a device.
/// </summary>
public readonly struct DeviceTiming
{
    internal DeviceTiming(int? powerOnDelay = default, int? shutdownDelay = default, int? sourceSwitchDelay = default)
    {
        (this.PowerOnDelay, this.ShutdownDelay, this.SourceSwitchDelay) = (powerOnDelay, shutdownDelay, sourceSwitchDelay);
    }

    /// <summary>
    /// Specifies the number of milliseconds NEEO should wait after powering on the device
    /// before sending it another command.
    /// </summary>
    [JsonPropertyName("standbyCommandDelay")]
    public int? PowerOnDelay { get; }

    /// <summary>
    /// Specifies the number of milliseconds NEEO should wait after shutting down the device
    /// before sending it another command.
    /// </summary>
    public int? ShutdownDelay { get; }

    /// <summary>
    /// Specifies the number of milliseconds NEEO should wait after switching input on the device
    /// before sending it another command.
    /// </summary>
    public int? SourceSwitchDelay { get; }
}
