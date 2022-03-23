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

internal sealed class RangeSensorDetails : SensorDetails, IRangeSensorDetails
{
    public RangeSensorDetails(IReadOnlyCollection<double> range, string unit)
        : base(SensorType.Range)
    {
        this.Range = range;
        this.Unit = Uri.EscapeDataString(unit);
    }

    public IReadOnlyCollection<double> Range { get; }

    public string Unit { get; }
}