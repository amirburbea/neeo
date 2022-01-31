using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An enumeration of NEEO component types.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<ComponentType>))]
public enum ComponentType
{
    /// <summary>
    /// A component for a standard button.
    /// </summary>
    [Text("button")]
    Button = 0,

    /// <summary>
    /// A component for a device supporting discovery, such as over a network.
    /// </summary>
    [Text("discover")]
    Discovery,

    [Text("directory")]
    Directory,

    [Text("favoritehandler")]
    FavoritesHandler,

    [Text("imageurl")]
    ImageUrl,

    [Text("register")]
    Registration,

    [Text("sensor")]
    Sensor,

    [Text("slider")]
    Slider,

    [Text("devicesubscription")]
    Subscription,

    [Text("switch")]
    Switch,

    [Text("textlabel")]
    TextLabel,
}