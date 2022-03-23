using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes the details of a slider.
/// </summary>
public interface ISliderDetails
{
    /// <summary>
    /// Gets the slider range.
    /// </summary>
    IReadOnlyCollection<double> Range { get; }

    /// <summary>
    /// Gets the name of the associated sensor.
    /// </summary>
    string SensorName { get; }

    /// <summary>
    /// Gets the type (a constant - &quot;range&quot;).
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the slider unit.
    /// </summary>
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