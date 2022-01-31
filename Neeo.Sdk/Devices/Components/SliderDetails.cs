using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

public interface ISliderDetails
{
    IReadOnlyCollection<double> Range { get; }

    string SensorName { get; }

    string Type { get; }

    string Unit { get; }
}

internal sealed record class SliderDetails(
    IReadOnlyCollection<double> Range, 
    string Unit, 
    [property: JsonPropertyName("sensor")] string SensorName
) : ISliderDetails
{
    public string Type => "range";
}