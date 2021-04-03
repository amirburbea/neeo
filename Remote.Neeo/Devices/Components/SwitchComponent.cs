namespace Remote.Neeo.Devices.Components
{
    public interface ISwitchComponent : IComponentWithAssociatedSensor
    {
    }

    internal sealed class SwitchComponent : Component,ISwitchComponent
    {
        public SwitchComponent(string name, string? label, string pathPrefix) 
            : base(ComponentType.Switch, name, label, pathPrefix)
        {
            this.SensorName = Component.GetAssociatedSensorName(name);
        }

        public string SensorName { get; }
    }
}