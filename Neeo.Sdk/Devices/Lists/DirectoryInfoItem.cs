using System;
using System.ComponentModel;

namespace Neeo.Sdk.Devices.Lists;

public sealed class DirectoryInfoItem(
    string title,
    string text,
    string? affirmativeButtonText = default,
    string? negativeButtonText = default,
    string? actionIdentifier = default
    ) : ClickableDirectoryItemBase(actionIdentifier), IDirectoryItem
{
    public string? AffirmativeButtonText { get; } = affirmativeButtonText;

    /// <summary>
    /// Tells the NEEO Brain that this is an info item.
    /// </summary>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsInfoItem { get; } = true;

    public string? NegativeButtonText { get; } = negativeButtonText;

    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));

    public string Title { get; } = title ?? throw new ArgumentNullException(nameof(text));

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.Info;
}
