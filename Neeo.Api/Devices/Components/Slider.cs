using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Components;

public interface ISlider
{
    string Type => "range";

    IReadOnlyCollection<double> Range { get; }

    [JsonPropertyName("sensor")]
    string SensorName { get; }

    string Unit { get; }
}

internal sealed record class Slider(IReadOnlyCollection<double> Range,  string Unit,string SensorName) : ISlider;