using System;
using System.Collections.Generic;

namespace Neeo.Api.Devices.Components;

public interface IRangeSensor : ISensor
{
    IReadOnlyCollection<double> Range { get; }

    string Unit { get; }
}

internal sealed record class RangeSensor : Sensor, IRangeSensor
{
    public RangeSensor(double low, double high, string? unit)
        : base(SensorTypes.Range)
    {
        this.Range = new[] { low, high };
        this.Unit = unit != null ? Uri.EscapeDataString(unit) : "%";
    }

    public IReadOnlyCollection<double> Range { get; init; }

    public string Unit { get; init; }
}