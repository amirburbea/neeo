using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes the details of a range sensor.
/// </summary>
public interface IRangeSensorDetails : ISensorDetails
{
    /// <summary>
    /// Gets the sensor range.
    /// </summary>
    IReadOnlyCollection<double> Range { get; }

    /// <summary>
    /// Gets the sensor unit.
    /// </summary>
    string Unit { get; }
}

internal sealed record class RangeSensorDetails(
    IReadOnlyCollection<double> Range,
    string Unit
) : SensorDetails(SensorType.Range), IRangeSensorDetails;