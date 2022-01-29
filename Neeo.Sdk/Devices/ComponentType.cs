using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An enumeration of NEEO component types.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<ComponentType>))]
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
