using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;


/// <summary>
/// Describes a text label component.
/// </summary>
public sealed record class TextLabelComponent(
    string Name,
    string? Label,
    string Path,
    bool? IsLabelVisible,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.TextLabel, Name, Label, Path);
