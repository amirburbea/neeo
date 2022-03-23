using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Components;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A model for a device adapter.
/// </summary>
public readonly struct DeviceModel : IComparable<DeviceModel>
{
    private readonly IDeviceAdapter _adapter;

    internal DeviceModel(int id, IDeviceAdapter adapter)
    {
        (this.Id, this._adapter) = (id, adapter);
        this.Device = new(adapter);
        this.Tokens = string.Join(' ', adapter.Tokens);
    }

    /// <summary>
    /// Gets the name of the device adapter.
    /// </summary>
    public string AdapterName => this._adapter.AdapterName;

    /// <summary>
    /// Gets the collection of device components.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public IReadOnlyCollection<IComponent> Components => this._adapter.Components;

    /// <summary>
    /// Identifying information about the device.
    /// </summary>
    public DeviceInfo Device { get; }

    /// <summary>
    /// Gets the collection of unique capabilities of the device.
    /// </summary>
    public IReadOnlyCollection<DeviceCapability> DeviceCapabilities => this._adapter.DeviceCapabilities;

    /// <summary>
    /// Gets the (optional) driver version.
    /// </summary>
    public int? DriverVersion => this._adapter.DriverVersion;

    /// <summary>
    /// Gets the (optional) device icon override.
    /// </summary>
    public DeviceIconOverride? Icon => this._adapter.Icon;

    /// <summary>
    /// Gets the device identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the device manufacturer.
    /// </summary>
    public string Manufacturer => this._adapter.Manufacturer;

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    public string Name => this._adapter.DeviceName;

    /// <summary>
    /// Gets information relating to device setup, specifically registration and discovery.
    /// </summary>
    public DeviceSetup Setup => this._adapter.Setup;

    /// <summary>
    /// Gets the set of delays NEEO should use when interacting with the device.
    /// </summary>
    public DeviceTiming Timing => this._adapter.Timing;

    /// <summary>
    /// Gets a string comprised of the search tokens delimited by a space.
    /// </summary>
    public string Tokens { get; }

    /// <summary>
    /// Gets the type of the device.
    /// </summary>
    public DeviceType Type => this._adapter.Type;

    int IComparable<DeviceModel>.CompareTo(DeviceModel other) => string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
}