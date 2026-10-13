using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;


/// <summary>
/// Describes an Image Url component.
/// </summary>
public sealed record class ImageUrlComponent(
    string Name,
    string? Label,
    string Path,
    ImageSize Size,
    [property: JsonPropertyName("sensor")] string SensorName
) : Component(ComponentType.ImageUrl, Name, Label, Path);
