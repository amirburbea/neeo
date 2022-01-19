using System;

namespace Neeo.Api.Devices.Components;

public interface ISliderComponent : IComponent
{
    ISlider Slider { get; }
}

internal sealed record class SliderComponent(
    String Name,
    String? Label,
    String Path,
    ISlider Slider
) : Component(ComponentType.Slider, Name, Label, Path), ISliderComponent;