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
    /// A component for a button.
    /// </summary>
    [Text("button")]
    Button = 0,

    /// <summary>
    /// A component for a device supporting discovery, such as over a network.
    /// </summary>
    [Text("discover")]
    Discovery,

    /// <summary>
    /// A component for a directory.
    /// </summary>
    [Text("directory")]
    Directory,

    /// <summary>
    /// A component for handling custom favorites.
    /// </summary>
    [Text("favoritehandler")]
    FavoritesHandler,

    /// <summary>
    /// A component for an image.
    /// </summary>
    [Text("imageurl")]
    ImageUrl,

    /// <summary>
    /// A component for a device supporting registration, either via a username and password or security code.
    /// </summary>
    [Text("register")]
    Registration,

    /// <summary>
    /// A component for a sensor.
    /// </summary>
    [Text("sensor")]
    Sensor,

    /// <summary>
    /// A component for a slider.
    /// </summary>
    [Text("slider")]
    Slider,

    /// <summary>
    /// A component for a driver that is notified of devices being added and removed.
    /// </summary>
    [Text("devicesubscription")]
    Subscription,

    /// <summary>
    /// A component for a toggle switch.
    /// </summary>
    [Text("switch")]
    Switch,

    /// <summary>
    /// A component for a text label.
    /// </summary>
    [Text("textlabel")]
    TextLabel,
}
