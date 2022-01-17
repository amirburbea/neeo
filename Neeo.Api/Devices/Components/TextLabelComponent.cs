namespace Neeo.Api.Devices.Components;

public interface ITextLabelComponent : IComponentWithAssociatedSensor
{
    bool? IsLabelVisible { get; }
}

internal sealed class TextLabelComponent : Component, ITextLabelComponent
{
    public TextLabelComponent(string name, string? label, string pathPrefix, bool? isLabelVisible)
        : base(ComponentType.ImageUrl, name, label, pathPrefix)
    {
        this.IsLabelVisible = isLabelVisible;
        this.SensorName = this.GetAssociatedSensorName();
    }

    public bool? IsLabelVisible { get; }

    public string SensorName { get; }
}
