using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a switch component.
/// </summary>
public sealed record class SwitchComponent(
    string Name,
    string? Label,
    string Path,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.Switch, Name, Label, Path);
