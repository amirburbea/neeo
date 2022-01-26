using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

public interface IRangeSensorDescriptor : ISensorDescriptor
{
    IReadOnlyCollection<double> Range { get; }

    string Unit { get; }
}

internal sealed record class RangeSensorDescriptor : SensorDescriptor, IRangeSensorDescriptor
{
    public RangeSensorDescriptor(double low, double high, string? unit)
        : base(SensorTypes.Range)
    {
        this.Range = new[] { low, high };
        this.Unit = unit != null ? Uri.EscapeDataString(unit) : "%";
    }

    public IReadOnlyCollection<double> Range { get; init; }

    public string Unit { get; init; }
}