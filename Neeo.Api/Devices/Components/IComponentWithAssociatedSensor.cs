using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Components;

public interface IComponentWithAssociatedSensor : IComponent
{
    string SensorName { get; }
}