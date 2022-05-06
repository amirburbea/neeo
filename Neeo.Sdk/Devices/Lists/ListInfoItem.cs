using System;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListInfoItem : ClickableListItemBase, IListItem
{
    public ListInfoItem(
        string title,
        string text,
        string? affirmativeButtonText = default,
        string? negativeButtonText = default,
        string? actionIdentifier = default
    ) : base(actionIdentifier)
    {
        this.Title = title ?? throw new ArgumentNullException(nameof(text));
        this.Text = text ?? throw new ArgumentNullException(nameof(text));
        this.AffirmativeButtonText = affirmativeButtonText;
        this.NegativeButtonText = negativeButtonText;
    }

    public string? AffirmativeButtonText { get; }

    /// <summary>
    /// Tells the NEEO Brain that this is an info item.
    /// </summary>
    public bool IsInfoItem { get; } = true;

    public string? NegativeButtonText { get; }

    public string Text { get; }

    public string Title { get; }

    ListItemType IListItem.Type => ListItemType.Info;
}