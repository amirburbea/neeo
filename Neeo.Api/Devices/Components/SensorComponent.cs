using System;

namespace Neeo.Api.Devices.Components;

public interface ISensorComponent : IComponent
{
    ISensor Sensor { get; }
}

internal sealed record class SensorComponent(String Name, String Label, String Path, ISensor Sensor) : Component(ComponentType.Sensor, Name, Label, Path), ISensorComponent;