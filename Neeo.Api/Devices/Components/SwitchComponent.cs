using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Components;

public interface ISwitchComponent : IComponent, IComponentWithAssociatedSensor
{
}

internal sealed record class SwitchComponent(
    string Name,
    string? Label,
    string Path,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.Switch, Name, Label, Path);