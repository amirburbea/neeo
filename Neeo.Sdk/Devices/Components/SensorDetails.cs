using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Components;

[JsonInterfaceSerializationConverter(typeof(ISensorDetails))]
public interface ISensorDetails
{
    SensorTypes Type { get; }
}

internal record class SensorDetails(SensorTypes Type) : ISensorDetails
{
    public const string ComponentSuffix = "_SENSOR";
}