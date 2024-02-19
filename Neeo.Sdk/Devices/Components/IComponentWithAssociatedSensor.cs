namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a component with an associated sensor name.
/// </summary>
public interface IComponentWithAssociatedSensor : IComponent
{
    /// <summary>
    /// Gets the name of the associated sensor.
    /// </summary>
    string SensorName { get; }
}
