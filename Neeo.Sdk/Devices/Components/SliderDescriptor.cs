using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

public interface ISliderDescriptor
{
    IReadOnlyCollection<double> Range { get; }

    [JsonPropertyName("sensor")]
    string SensorName { get; }

    string Type => "range";

    string Unit { get; }
}

internal sealed record class SliderDescriptor(IReadOnlyCollection<double> Range, string Unit, string SensorName) : ISliderDescriptor;