using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

public interface ISliderComponent : IComponent
{
    ISliderDescriptor Slider { get; }
}

internal sealed record class SliderComponent : Component, ISliderComponent
{
    public ISliderDescriptor Slider { get; }

    public SliderComponent(string name, string? label, string path, SliderDescriptor slider)
        : base(ComponentType.Slider, name, label, path)
    {
        this.Slider = slider;
    }
}