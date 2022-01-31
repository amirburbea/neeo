using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Components;

namespace Neeo.Sdk.Devices;

public interface IDeviceModel : IComparable<IDeviceModel>
{
    string AdapterName { get; }

    [JsonPropertyName("capabilities")]
    IReadOnlyCollection<IComponent> Components { get; }

    IDeviceInfo Device { get; }

    IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    int DriverVersion { get; }

    int Id { get; }

    string Manufacturer { get; }

    string Name { get; }

    DeviceSetup Setup { get; }

    DeviceTiming Timing { get; }

    string Tokens { get; }

    DeviceType Type { get; }

    int IComparable<IDeviceModel>.CompareTo(IDeviceModel? other) => StringComparer.OrdinalIgnoreCase.Compare(this.Name, other?.Name);
}

internal sealed class DeviceModel : IDeviceModel
{
    private readonly IDeviceAdapter _adapter;

    public DeviceModel(int id, IDeviceAdapter adapter)
    {
        (this.Id, this._adapter) = (id, adapter);
        this.Device = new(adapter.DeviceName, adapter.Tokens, adapter.SpecificName, adapter.Icon);
        this.Tokens = string.Join(' ', adapter.Tokens);
    }

    public string AdapterName => this._adapter.AdapterName;

    public IReadOnlyCollection<IComponent> Components => this._adapter.Components;

    IDeviceInfo IDeviceModel.Device => this.Device;

    public DeviceInfo Device { get; }

    public IReadOnlyCollection<DeviceCapability> DeviceCapabilities => this._adapter.DeviceCapabilities;

    public int DriverVersion => this._adapter.DriverVersion ?? 0;

    public int Id { get; }

    public string Manufacturer => this._adapter.Manufacturer;

    public string Name => this._adapter.DeviceName;

    public DeviceSetup Setup => this._adapter.Setup;

    public DeviceTiming Timing => this._adapter.Timing;

    public string Tokens { get; }

    public DeviceType Type => this._adapter.Type;

    public record struct DeviceInfo(string Name, IReadOnlyCollection<string> Tokens, string? SpecificName, DeviceIconOverride? Icon) : IDeviceInfo;
}