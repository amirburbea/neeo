using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi.Models;

[JsonConverter(typeof(TextJsonConverter<PlayerType>))]
internal enum PlayerType
{
    [Text("audio")]
    Audio,
    [Text("picture")]
    Picture,
    [Text("video")]
    Video,
}