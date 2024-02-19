using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi.Models;

[JsonConverter(typeof(TextJsonConverter<ItemType>))]
internal enum ItemType
{
    [Text("episode")]
    Episode = 1,

    [Text("movie")]
    Movie,

    [Text("musicvideo")]
    MusicVideo,

    [Text("picture")]
    Picture,

    [Text("song")]
    Song,
}
