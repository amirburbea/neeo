using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Components;

public interface IRangeSensorDetails : ISensorDetails
{
    IReadOnlyCollection<double> Range { get; }

    string Unit { get; }
}

internal sealed record class RangeSensorDetails(
    IReadOnlyCollection<double> Range, 
    string Unit
) : SensorDetails(SensorType.Range), IRangeSensorDetails;