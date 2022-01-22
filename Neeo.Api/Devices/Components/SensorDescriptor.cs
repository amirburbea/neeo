using Neeo.Api.Json;

namespace Neeo.Api.Devices.Components;

[JsonInterfaceSerializationConverter(typeof(ISensorDescriptor))]
public interface ISensorDescriptor
{
    SensorTypes Type { get; }
}

internal record class SensorDescriptor(SensorTypes Type) : ISensorDescriptor
{
    public const string ComponentSuffix = "_SENSOR";
}