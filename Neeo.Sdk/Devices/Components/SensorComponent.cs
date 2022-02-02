namespace Neeo.Sdk.Devices.Components;

public interface ISensorComponent : IComponent
{
    ISensorDetails Sensor { get; }
}

internal sealed record class SensorComponent(
    string Name,
    string Label,
    string Path,
    ISensorDetails Sensor
) : Component(ComponentType.Sensor, Name, Label, Path), ISensorComponent;