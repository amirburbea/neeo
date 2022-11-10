using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Interface for a list item.
/// </summary>
[JsonDirectSerialization<IListItem>]
public interface IListItem
{
    /// <summary>
    /// Gets the type of the list item.
    /// </summary>
    ListItemType Type { get; }
}
