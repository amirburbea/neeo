using System.Collections.Generic;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Describes a device adapter.
/// </summary>
public interface IDeviceAdapter
{
    /// <summary>
    /// Gets the name of the device adapter.
    /// </summary>
    string AdapterName { get; }

    /// <summary>
    /// Gets the collection of device components.
    /// </summary>
    IReadOnlyCollection<IComponent> Components { get; }

    /// <summary>
    /// Gets the collection of unique capabilities of the device.
    /// </summary>
    IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Gets the (optional) driver version.
    /// </summary>
    int? DriverVersion { get; }

    /// <summary>
    /// Gets the (optional) device icon override.
    /// </summary>
    DeviceIconOverride? Icon { get; }

    /// <summary>
    /// Gets the (optional) device initialization function.
    /// </summary>
    DeviceInitializer? Initializer { get; }

    /// <summary>
    /// Gets the device manufacturer.
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// Gets the (optional) device route handler which can handle Http requests with a specific prefix.
    /// </summary>
    DeviceRouteHandler? RouteHandler { get; }

    /// <summary>
    /// Gets information relating to device setup, specifically registration and discovery.
    /// </summary>
    DeviceSetup Setup { get; }

    /// <summary>
    /// Gets the specific name override (typically if unspecified something more generic such as TV would appear on the remote).
    /// </summary>
    string? SpecificName { get; }

    /// <summary>
    /// Gets the set of delays NEEO should use when interacting with the device.
    /// </summary>
    DeviceTiming Timing { get; }

    /// <summary>
    /// Gets the collection of additional search tokens.
    /// </summary>
    IReadOnlyCollection<string> Tokens { get; }

    /// <summary>
    /// Gets the type of the device.
    /// </summary>
    DeviceType Type { get; }

    /// <summary>
    /// When device routes are enabled (via a call to <see cref="IDeviceBuilder.EnableDeviceRoute"/>)
    /// this would be the callback to notify the device adapter of its URI prefix, otherwise <see langword="null"/>.
    /// </summary>
    UriPrefixCallback? UriPrefixCallback { get; }

    /// <summary>
    /// Gets the feature (if it exists) for the device adapter with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the feature.</param>
    /// <returns>Feature (or <see langword="null"/> if it does not exist).</returns>
    IFeature? GetFeature(string name);

    /// <summary>
    /// Gets the feature (if it exists) for the device adapter with the specified component <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The component type for which to fetch the assciated feature.</param>
    /// <returns>Feature (or <see langword="null"/> if it does not exist).</returns>
    IFeature? GetFeature(ComponentType type) => this.GetFeature(TextAttribute.GetText(type));
}

internal record struct DeviceAdapter(
    string AdapterName,
    IReadOnlyCollection<IComponent> Components,
    IReadOnlyDictionary<string, IFeature> Features,
    IReadOnlyCollection<DeviceCapability> DeviceCapabilities,
    string DeviceName,
    int? DriverVersion,
    DeviceIconOverride? Icon,
    DeviceInitializer? Initializer,
    string Manufacturer,
    DeviceRouteHandler? RouteHandler,
    DeviceSetup Setup,
    string? SpecificName,
    DeviceTiming Timing,
    IReadOnlyCollection<string> Tokens,
    DeviceType Type,
    UriPrefixCallback? UriPrefixCallback
) : IDeviceAdapter
{
    public IFeature? GetFeature(string name) => this.Features.GetValueOrDefault(name);
}