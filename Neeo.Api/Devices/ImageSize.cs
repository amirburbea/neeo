using System.Text.Json.Serialization;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

/// <summary>
/// Image size in the UI.
/// The small image has the size of a button while the large image is a square image using the full width of the client.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<ImageSize>))]
public enum ImageSize
{
    /// <summary>
    /// A small image the size of a button.
    /// </summary>
    /// <remarks>
    /// Typically an image of approximately 100x100px is appropriate.
    /// </remarks>
    [Text("small")]
    Small = 0,

    /// <summary>
    /// A large image the full width of the client.
    /// </summary>
    /// <remarks>
    /// Typically an image of approximately 480x480px is appropriate.
    /// </remarks>
    [Text("large")]
    Large
}
