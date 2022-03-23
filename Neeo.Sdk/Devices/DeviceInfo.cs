using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Identifying information about a device.
/// </summary>
public readonly struct DeviceInfo
{
    private readonly IDeviceAdapter _adapter;

    internal DeviceInfo(IDeviceAdapter adapter) => this._adapter = adapter;

    /// <summary>
    /// Gets the device adapter name.
    /// </summary>
    public string Name => this._adapter.DeviceName;

    /// <summary>
    /// Gets the optional specific name specified for the device.
    /// </summary>
    [JsonPropertyName("specificname")]
    public string? SpecificName => this._adapter.SpecificName;

    /// <summary>
    /// Gets the collection of additional search tokens specified for the device.
    /// </summary>
    public IReadOnlyCollection<string> Tokens => this._adapter.Tokens;
}