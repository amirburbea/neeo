namespace Remote.Neeo.Devices.Components;

public interface IImageUrlComponent : IComponentWithAssociatedSensor
{
    ImageSize Size { get; }
}

internal sealed class ImageUrlComponent : Component, IImageUrlComponent
{
    public ImageUrlComponent(string name, string? label, string pathPrefix, ImageSize size)
        : base(ComponentType.ImageUrl, name, label, pathPrefix)
    {
        this.Size = size;
        this.SensorName = Component.GetAssociatedSensorName(name);
    }

    public string SensorName { get; }

    public ImageSize Size { get; }
}
