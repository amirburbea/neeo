using System;
using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Components;

public interface IImageUrlComponent : IComponent, IComponentWithAssociatedSensor
{
    string? ImageUri { get; }

    ImageSize Size { get; }
}

internal sealed record class ImageUrlComponent(
    String Name,
    String? Label,
    String Path,
    String? ImageUri,
    ImageSize Size,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.ImageUrl, Name, Label, Path), IImageUrlComponent;