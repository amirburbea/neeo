using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes the details of a sensor.
/// </summary>
[JsonDirectSerialization(typeof(ISensorDetails))]
public interface ISensorDetails
{
    /// <summary>
    /// Gets the type of the sensor.
    /// </summary>
    SensorType Type { get; }
}

internal class SensorDetails : ISensorDetails
{
    public const string ComponentSuffix = "_SENSOR";

    public SensorDetails(SensorType type) => this.Type = type;

    public SensorType Type { get; }
}