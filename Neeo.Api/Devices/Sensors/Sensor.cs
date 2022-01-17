using System;
using System.Collections.Generic;

namespace Neeo.Api.Devices.Sensors;

public interface IRangeSensor : ISensor
{
    IReadOnlyCollection<double> Range { get; }

    string Unit { get; }
}

public interface ISensor
{
    SensorType Type { get; }
}

internal class RangeSensor : Sensor, IRangeSensor
{
    public RangeSensor(double low, double high, string? unit)
        : base(SensorType.Range)
    {
        this.Range = new[] { low, high };
        this.Unit = unit != null ? Uri.EscapeDataString(unit) : "%";
    }

    public IReadOnlyCollection<double> Range { get; }

    public string Unit { get; }
}

internal class Sensor : ISensor
{
    public Sensor(SensorType type) => this.Type = type;

    public SensorType Type { get; }
}
