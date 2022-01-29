using System;

namespace Neeo.Sdk.Devices.Components;

public interface ISensorComponent : IComponent
{
    ISensorDetails Sensor { get; }
}

internal sealed record class SensorComponent(
    String Name,
    String Label,
    String Path,
    ISensorDetails Sensor
) : Component(ComponentType.Sensor, Name, Label, Path), ISensorComponent;