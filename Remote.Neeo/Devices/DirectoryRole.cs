using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices;

[JsonConverter(typeof(TextAttribute.EnumJsonConverter<DirectoryRole>))]
public enum DirectoryRole
{
    [Text("ROOT")]
    Root = 0,

    [Text("QUEUE")]
    Queue
}
