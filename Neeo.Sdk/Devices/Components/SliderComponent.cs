using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Components;

[JsonInterfaceSerializationConverter(typeof(IComponent))]
public interface ISliderComponent : IComponent
{
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