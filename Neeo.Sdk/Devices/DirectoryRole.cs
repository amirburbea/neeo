using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

[JsonConverter(typeof(TextJsonConverter<DirectoryRole>))]
public enum DirectoryRole
{
    [Text("ROOT")]
    Root = 1,

    [Text("QUEUE")]
    Queue = 2
}
