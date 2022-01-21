using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Components;

public interface ISliderComponent : IComponent
{
    ISliderDescriptor Slider { get; }
}

public interface ISliderDescriptor
{
    IReadOnlyCollection<double> Range { get; }

    [JsonPropertyName("sensor")]
    string SensorName { get; }

    string Type => "range";

    string Unit { get; }
}

internal sealed record class SliderComponent : Component, ISliderComponent
{
    public ISliderDescriptor Slider { get; }

    public SliderComponent(string name, string? label, string path, IReadOnlyCollection<double> range, string unit, string sensorName)
        : base(ComponentType.Slider, name, label, path)
    {
        this.Slider = new SliderDescriptor(range, unit, sensorName);
    }
}

internal sealed record class SliderDescriptor(IReadOnlyCollection<double> Range, string Unit, string SensorName) : ISliderDescriptor;