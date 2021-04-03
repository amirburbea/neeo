using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Components
{
    public interface IComponentWithAssociatedSensor : IComponent
    {
        [JsonPropertyName("sensor")]
        string SensorName { get; }
    }
}