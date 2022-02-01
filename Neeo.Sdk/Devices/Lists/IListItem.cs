using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Lists;

[JsonDirectSerialization(typeof(IListItem))]
public interface IListItem
{
    ListItemType Type { get; }
}
