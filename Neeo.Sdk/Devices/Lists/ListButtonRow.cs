using System.Collections.Generic;
using System.Linq;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListButtonRow : IListItem
{
    internal ListButtonRow(IEnumerable<ListButton> buttons) => this.Buttons = buttons.ToArray();

    public IReadOnlyCollection<ListButton> Buttons { get; }

    ListItemType IListItem.Type => ListItemType.ButtonRow;
}
