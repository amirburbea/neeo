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

internal record class SensorDetails(SensorType Type) : ISensorDetails
{
    public const string ComponentSuffix = "_SENSOR";
}