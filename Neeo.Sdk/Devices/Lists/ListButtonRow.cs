using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// A row of <see cref="ListButton"/> buttons.
/// </summary>
public sealed class ListButtonRow
{
    internal ListButtonRow(ListButton[] buttons) => this.Buttons = buttons ?? throw new ArgumentNullException(nameof(buttons));

    /// <summary>
    /// Gets the collection of buttons in the row.
    /// </summary>
    public IReadOnlyCollection<ListButton> Buttons { get; }
}