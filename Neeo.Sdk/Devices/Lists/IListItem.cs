using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

[JsonDirectSerialization(typeof(IListItem))]
public interface IListItem
{
    ListItemType Type { get; }
}
