using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Components;

/// <summary>
/// An enumeration of NEEO component types.
/// </summary>
[JsonConverter(typeof(TextAttribute.EnumJsonConverter<ComponentType>))]
public enum ComponentType
{
    [Text("button")]
    Button = 0,

    [Text("discover")]
    Discovery,

    [Text("directory")]
    Directory,

    [Text("favoritehandler")]
    FavoritesHandler,

    [Text("imageurl")]
    ImageUrl,

    [Text("power")]
    Power,

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
