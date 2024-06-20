using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Lists;

public sealed class DirectoryButtonRow : IDirectoryItem
{
    internal DirectoryButtonRow(IEnumerable<DirectoryButton> buttons) => this.Buttons = [.. buttons];

    public IReadOnlyCollection<DirectoryButton> Buttons { get; }

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.ButtonRow;
}
