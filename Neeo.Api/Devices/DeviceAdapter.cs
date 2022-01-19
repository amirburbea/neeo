using System.Collections.Generic;
using Neeo.Api.Devices.Components;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

public interface IDeviceAdapter
{
    string AdapterName { get; }

    IReadOnlyCollection<IComponent> Capabilities { get; }

    IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    string DeviceName { get; }

    uint? DriverVersion { get; }

    IReadOnlyDictionary<string, ICapabilityHandler> CapabilityHandlers { get; }

    DeviceIconOverride? Icon { get; }

    DeviceInitializer? Initializer { get; }

    string Manufacturer { get; }

    IDeviceSetup Setup { get; }

    string? SpecificName { get; }

    DeviceTiming Timing { get; }

    IReadOnlyCollection<string> Tokens { get; }

    DeviceType Type { get; }

    ICapabilityHandler? GetCapabilityHandler(ComponentType type) => this.GetCapabilityHandler(TextAttribute.GetText(type));

    ICapabilityHandler? GetCapabilityHandler(string name) => this.CapabilityHandlers.GetValueOrDefault(name);
}

internal sealed class DeviceAdapter : IDeviceAdapter
{
    public DeviceAdapter(
        string adapterName,
        DeviceType type,
        string manufacturer,
        uint? driverVersion,
        DeviceTiming? timing,
        IReadOnlyCollection<DeviceCapability> deviceCapabilities,
        IDeviceSetup setup,
        DeviceInitializer? initializer,
        string deviceName,
        IReadOnlyCollection<string> tokens,
        string? specificName,
        DeviceIconOverride? icon,
        IReadOnlyCollection<IComponent> capabilities,
        IReadOnlyDictionary<string, ICapabilityHandler> capabilityHandlers)
    {
        this.AdapterName = adapterName;
        this.Type = type;
        this.Manufacturer = manufacturer;
        this.Initializer = initializer;
        this.Timing = timing ?? new();
        this.DeviceCapabilities = deviceCapabilities;
        this.DriverVersion = driverVersion;
        this.Setup = setup;
        this.DeviceName = deviceName;
        this.Tokens = tokens;
        this.Capabilities = capabilities;
        this.CapabilityHandlers = capabilityHandlers;
        this.Icon = icon;
        this.SpecificName = specificName;
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<IComponent> Capabilities { get; }

    public IReadOnlyDictionary<string, ICapabilityHandler> CapabilityHandlers { get; }

    public IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    public string DeviceName { get; }

    public uint? DriverVersion { get; }

    public DeviceIconOverride? Icon { get; }

    public DeviceInitializer? Initializer { get; }

    public string Manufacturer { get; }

    public IDeviceSetup Setup { get; }

    public string? SpecificName { get; }

    public DeviceTiming Timing { get; }

    public IReadOnlyCollection<string> Tokens { get; }

    public DeviceType Type { get; }
}