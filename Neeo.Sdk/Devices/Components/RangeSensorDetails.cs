using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

public interface IRangeSensorDetails : ISensorDetails
{
    IReadOnlyCollection<double> Range { get; }

    string Unit { get; }
}

internal sealed record class RangeSensorDetails : SensorDetails, IRangeSensorDetails
{
    public RangeSensorDetails(double low, double high, string? unit)
        : base(SensorTypes.Range)
    {
        this.Range = new[] { low, high };
        this.Unit = unit != null ? Uri.EscapeDataString(unit) : "%";
    }

    public IReadOnlyCollection<double> Range { get; init; }

    public string Unit { get; init; }
}