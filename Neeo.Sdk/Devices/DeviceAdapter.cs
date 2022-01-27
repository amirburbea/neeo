using System;
using System.Collections.Generic;
using System.Linq;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Controllers;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

using static KnownButtons;

public interface IDeviceAdapter
{
    string AdapterName { get; }

    IReadOnlyCollection<IComponent> Capabilities { get; }

    IReadOnlyDictionary<string, IController> CapabilityHandlers { get; }

    IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    string DeviceName { get; }

    uint? DriverVersion { get; }

    DeviceIconOverride? Icon { get; }

    DeviceInitializer? Initializer { get; }

    string Manufacturer { get; }

    IDeviceSetup Setup { get; }

    string? SpecificName { get; }

    DeviceTiming Timing { get; }

    IReadOnlyCollection<string> Tokens { get; }

    DeviceType Type { get; }

    IController? GetCapabilityHandler(ComponentType type);

    IController? GetCapabilityHandler(string name);
}

internal record DeviceAdapter(
    string AdapterName,
    IReadOnlyCollection<IComponent> Capabilities,
    IReadOnlyDictionary<string, IController> CapabilityHandlers,
    IReadOnlyCollection<DeviceCapability> DeviceCapabilities,
    string DeviceName,
    uint? DriverVersion,
    DeviceIconOverride? Icon,
    DeviceInitializer? Initializer,
    string Manufacturer,
    IDeviceSetup Setup,
    string? SpecificName,
    DeviceTiming Timing,
    IReadOnlyCollection<string> Tokens,
    DeviceType Type
) : IDeviceAdapter
{
    public IController? GetCapabilityHandler(ComponentType type) => this.GetCapabilityHandler(TextAttribute.GetText(type));

    public IController? GetCapabilityHandler(string name) => this.CapabilityHandlers.GetValueOrDefault(name);

    
}