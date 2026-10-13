using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a slider component.
/// </summary>
public sealed record class SliderComponent(
    string Name,
    string? Label,
    string Path,
    SliderDetails Slider
) : Component(ComponentType.Slider, Name, Label, Path);

/// <summary>
/// Describes the details of a slider.
/// </summary>
public readonly record struct SliderDetails(
    IReadOnlyCollection<double> Range,
    string Unit,
    [property: JsonPropertyName("sensor")] string SensorName
)
{
    /// <summary>
    /// Gets the type (a constant - "range").
    /// </summary>
    public string Type { get; } = "range";
}
