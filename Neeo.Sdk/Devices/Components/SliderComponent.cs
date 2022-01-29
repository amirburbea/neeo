using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

public interface ISliderComponent : IComponent
{
    ISliderDetails Slider { get; }
}

internal sealed record class SliderComponent : Component, ISliderComponent
{
    public ISliderDetails Slider { get; }

    public SliderComponent(string name, string? label, string path, SliderDetails slider)
        : base(ComponentType.Slider, name, label, path)
    {
        this.Slider = slider;
    }
}