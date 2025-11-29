using System.Collections.Generic;
using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Components;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A model for a built device.
/// </summary>
public readonly struct DeviceModel(int id, IDeviceAdapter adapter)
{
    /// <summary>
    /// Gets the name of the device adapter.
    /// </summary>
    public string AdapterName => adapter.AdapterName;

    /// <summary>
    /// Gets the collection of device components.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public IReadOnlyCollection<Component> Components => adapter.Components;

    /// <summary>
    /// Gets the collection of unique capabilities of the device.
    /// </summary>
    public IReadOnlyCollection<DeviceCapability> DeviceCapabilities => adapter.DeviceCapabilities;

    /// <summary>
    /// Gets the (optional) driver version.
    /// </summary>
    public int? DriverVersion => adapter.DriverVersion;

    /// <summary>
    /// Gets the (optional) device icon override.
    /// </summary>
    public DeviceIconOverride? Icon => adapter.Icon;

    /// <summary>
    /// Gets the device identifier.
    /// </summary>
    public int Id => id;

    /// <summary>
    /// Identifying information about the device.
    /// </summary>
    [JsonPropertyName("device")]
    public DeviceInfo Info { get; } = new(adapter);

    /// <summary>
    /// Gets the device manufacturer.
    /// </summary>
    public string Manufacturer => adapter.Manufacturer;

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    public string Name => adapter.DeviceName;

    /// <summary>
    /// Gets information relating to device setup, specifically registration and discovery.
    /// </summary>
    public DeviceSetup Setup => adapter.Setup;

    /// <summary>
    /// Gets the set of delays NEEO should use when interacting with the device.
    /// </summary>
    public DeviceTiming? Timing => adapter.Timing;

    /// <summary>
    /// Gets a string comprised of the search tokens delimited by a space.
    /// </summary>
    public string Tokens { get; } = string.Join(' ', adapter.Tokens);

    /// <summary>
    /// Gets the type of the device.
    /// </summary>
    public DeviceType Type => adapter.Type;
}
