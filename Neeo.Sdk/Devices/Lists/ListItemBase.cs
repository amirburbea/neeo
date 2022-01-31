using System.Text.Json.Serialization;
using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Lists;

[JsonDirectSerialization(typeof(ListItemBase))]
public abstract class ListItemBase
{
    public ListItemBase(ListItemType type) => this.Type = type;

    [JsonIgnore]
    public ListItemType Type { get; }
}

public enum ListItemType
{
    Entry,
    Header,
    Info,
    TileRow,
    ButtonRow,
}