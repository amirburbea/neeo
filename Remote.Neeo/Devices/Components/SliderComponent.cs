namespace Remote.Neeo.Devices.Components;

public interface ISliderComponent : IComponentWithAssociatedSensor
{
}

internal sealed class SliderComponent : Component, ISliderComponent
{
    public SliderComponent(string name, string? label, string pathPrefix)
        : base(ComponentType.Slider, name, label, pathPrefix)
    {
        this.SensorName = Component.GetAssociatedSensorName(name);
    }

    public string SensorName { get; }
}
