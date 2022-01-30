using System.Collections.Generic;
using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

public interface IDeviceAdapter
{
    string AdapterName { get; }

    IReadOnlyDictionary<string, IFeature> Features { get; }

    IReadOnlyCollection<IComponent> Components { get; }

    IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    string DeviceName { get; }

    uint? DriverVersion { get; }

    DeviceIconOverride? Icon { get; }

    DeviceInitializer? Initializer { get; }

    string Manufacturer { get; }

    DeviceSetup Setup { get; }

    string? SpecificName { get; }

    DeviceTiming Timing { get; }

    IReadOnlyCollection<string> Tokens { get; }

    DeviceType Type { get; }

    IFeature? GetFeature(ComponentType type) => this.GetFeature(TextAttribute.GetText(type));

    IFeature? GetFeature(string name) => this.Features.GetValueOrDefault(name);
}

internal sealed record class DeviceAdapter(
    string AdapterName,
    IReadOnlyCollection<IComponent> Components,
    IReadOnlyDictionary<string, IFeature> Features,
    IReadOnlyCollection<DeviceCapability> DeviceCapabilities,
    string DeviceName,
    uint? DriverVersion,
    DeviceIconOverride? Icon,
    DeviceInitializer? Initializer,
    string Manufacturer,
    DeviceSetup Setup,
    string? SpecificName,
    DeviceTiming Timing,
    IReadOnlyCollection<string> Tokens,
    DeviceType Type
) : IDeviceAdapter;