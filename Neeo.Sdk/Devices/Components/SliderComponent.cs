namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a slider component.
/// </summary>
public interface ISliderComponent : IComponent
{
    /// <summary>
    /// Gets the details of the slider..
    /// </summary>
    ISliderDetails Slider { get; }
}

internal sealed record class SliderComponent(
    string Name,
    string? Label,
    string Path,
    SliderDetails Slider
) : Component(ComponentType.Slider, Name, Label, Path), ISliderComponent
{
    ISliderDetails ISliderComponent.Slider => this.Slider;
}