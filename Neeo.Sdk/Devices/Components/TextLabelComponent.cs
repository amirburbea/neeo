using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a text label component.
/// </summary>
public interface ITextLabelComponent : IComponent, IComponentWithAssociatedSensor
{
    /// <summary>
    /// Gets a value indicating if the text label should be visible.
    /// </summary>
    /// <remarks>A value of <see langword="null" /> is translated to mean <see langword="true" />.</remarks>
    bool? IsLabelVisible { get; }
}

internal sealed record class TextLabelComponent(
    string Name,
    string? Label,
    string Path,
    bool? IsLabelVisible,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.TextLabel, Name, Label, Path), ITextLabelComponent;
