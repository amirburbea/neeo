using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

public interface IRangeSensorDetails : ISensorDetails
{
    IReadOnlyCollection<double> Range { get; }

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