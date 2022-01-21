using System;

namespace Neeo.Api.Devices.Components;

public interface ISensorComponent : IComponent
{
    ISensorDescriptor Sensor { get; }
}

internal sealed record class SensorComponent(String Name, String Label, String Path, ISensorDescriptor Sensor) : Component(ComponentType.Sensor, Name, Label, Path), ISensorComponent;