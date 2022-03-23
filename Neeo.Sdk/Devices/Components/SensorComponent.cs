namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a device sensor.
/// </summary>
public interface ISensorComponent : IComponent
{
    /// <summary>
    /// Gets the sensor details.
    /// </summary>
    ISensorDetails Sensor { get; }
}

internal sealed record class SensorComponent(
    string Name,
    string Label,
    string Path,
    ISensorDetails Sensor
) : Component(ComponentType.Sensor, Name, Label, Path), ISensorComponent;