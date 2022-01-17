using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Components;

public interface IComponentWithAssociatedSensor : IComponent
{
    [JsonPropertyName("sensor")]
    string SensorName { get; }
}
