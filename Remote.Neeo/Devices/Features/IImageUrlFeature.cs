using Remote.Neeo.Json;

namespace Remote.Neeo.Devices.Features
{
    [JsonInterfaceConverter(typeof(IImageUrlFeature))]
    public interface IImageUrlFeature : IFeature
    {
        /// <summary>
        /// Gets the image size in the UI.
        /// The small image has the size of a button while the large image is a square image using the full width of the client.
        /// </summary>
        ImageSize Size { get; }
    }
}