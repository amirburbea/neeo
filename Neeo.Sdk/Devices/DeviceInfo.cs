using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Identifying information about a device.
/// </summary>
/// <param name="adapter">The device adapter being described.</param>
public readonly struct DeviceInfo(IDeviceAdapter adapter)
{
    /// <summary>
    /// Gets the device adapter name.
    /// </summary>
    public string Name => adapter.DeviceName;

    /// <summary>
    /// Gets the optional specific name specified for the device.
    /// </summary>
    [JsonPropertyName("specificname")]
    public string? SpecificName => adapter.SpecificName;

    /// <summary>
    /// Gets the collection of additional search tokens specified for the device.
    /// </summary>
    public IReadOnlyCollection<string> Tokens => adapter.Tokens;
}
