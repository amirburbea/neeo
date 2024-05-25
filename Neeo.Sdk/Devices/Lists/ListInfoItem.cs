using System;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListInfoItem(
    string title,
    string text,
    string? affirmativeButtonText = default,
    string? negativeButtonText = default,
    string? actionIdentifier = default
    ) : ClickableListItemBase(actionIdentifier), IListItem
{
    public string? AffirmativeButtonText { get; } = affirmativeButtonText;

    /// <summary>
    /// Tells the NEEO Brain that this is an info item.
    /// </summary>
    public bool IsInfoItem { get; } = true;

    public string? NegativeButtonText { get; } = negativeButtonText;

    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));

    public string Title { get; } = title ?? throw new ArgumentNullException(nameof(text));

    ListItemType IListItem.Type => ListItemType.Info;
}
