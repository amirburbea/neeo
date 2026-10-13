using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a device component.
/// </summary>
[JsonDerivedType(typeof(DirectoryComponent))]
[JsonDerivedType(typeof(ImageUrlComponent))]
[JsonDerivedType(typeof(SensorComponent))]
[JsonDerivedType(typeof(SliderComponent))]
[JsonDerivedType(typeof(SwitchComponent))]
[JsonDerivedType(typeof(TextLabelComponent))]
public record class Component(
    ComponentType Type,
    string Name,
    string? Label,
    string Path
);
