using Neeo.Api.Json;

namespace Neeo.Api.Devices.Components;

[JsonInterfaceSerializationConverter(typeof(ISensor))]
public interface ISensor
{
    SensorTypes Type { get; }
}

internal record class Sensor(SensorTypes Type) : ISensor
{
    public const string ComponentSuffix = "_SENSOR";
}