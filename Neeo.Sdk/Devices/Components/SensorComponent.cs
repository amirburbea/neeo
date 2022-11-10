using Neeo.Sdk.Utilities;

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
) : Component(ComponentType.Sensor, Name, Label, Path), ISensorComponent
{
    public const string ComponentSuffix = "_SENSOR";
}

/// <summary>
/// Describes the details of a sensor.
/// </summary>
[JsonDirectSerialization<ISensorDetails>]
public interface ISensorDetails
{
    /// <summary>
    /// Gets the type of the sensor.
    /// </summary>
    SensorType Type { get; }
}

internal record class SensorDetails(SensorType Type) : ISensorDetails;