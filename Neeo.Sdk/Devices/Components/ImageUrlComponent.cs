using System;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes an Image Url component.
/// </summary>
public interface IImageUrlComponent : IComponent, IComponentWithAssociatedSensor
{
    /// <summary>
    /// Gets the size of the image.
    /// </summary>
    ImageSize Size { get; }
}

internal sealed record class ImageUrlComponent(
    String Name,
    String? Label,
    String Path,
    ImageSize Size,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.ImageUrl, Name, Label, Path), IImageUrlComponent;