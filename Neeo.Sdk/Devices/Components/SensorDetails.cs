using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Components;

[JsonDirectSerialization(typeof(ISensorDetails))]
public interface ISensorDetails
{
    SensorType Type { get; }
}

internal class SensorDetails : ISensorDetails
{
    public const string ComponentSuffix = "_SENSOR";

    public SensorDetails(SensorType type) => this.Type = type;

    public SensorType Type { get; }
}