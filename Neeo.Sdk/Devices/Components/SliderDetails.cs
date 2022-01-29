using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

public interface ISliderDetails
{
    IReadOnlyCollection<double> Range { get; }

    [JsonPropertyName("sensor")]
    string SensorName { get; }

    string Type => "range";

    string Unit { get; }
}

internal sealed record class SliderDetails(IReadOnlyCollection<double> Range, string Unit, string SensorName) : ISliderDetails;