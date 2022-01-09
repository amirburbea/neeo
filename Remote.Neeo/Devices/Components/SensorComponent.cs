using Remote.Neeo.Devices.Sensors;

namespace Remote.Neeo.Devices.Components;

public interface ISensorComponent : IComponent
{
    ISensor Sensor { get; }
}

internal sealed class SensorComponent : Component, ISensorComponent
{
    public SensorComponent(string name, string? label, string pathPrefix, Sensor sensor)
        : base(ComponentType.Sensor, name, label, pathPrefix)
    {
        this.Sensor = sensor;
    }

    ISensor ISensorComponent.Sensor => this.Sensor;

    public Sensor Sensor { get; }
}
