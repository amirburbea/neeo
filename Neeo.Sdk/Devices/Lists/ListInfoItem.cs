using System;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListInfoItem : ListItemBase
{
    public ListInfoItem(
        string title,
        string text,
        string? affirmativeButtonText = default,
        string? negativeButtonText = default,
        string? actionIdentifier = default
    ) : base(ListItemType.Info)
    {
        this.Title = title ?? throw new ArgumentNullException(nameof(text));
        this.Text = text ?? throw new ArgumentNullException(nameof(text));
        this.ActionIdentifier = actionIdentifier;
        this.AffirmativeButtonText = affirmativeButtonText;
        this.NegativeButtonText = negativeButtonText;
    }

    public string? ActionIdentifier { get; }

    public string? AffirmativeButtonText { get; }

    /// <summary>
    /// Tells the NEEO Brain that this is an info item.
    /// </summary>
    public bool IsInfoItem { get; } = true;

    public string? NegativeButtonText { get; }

    public string Text { get; }

    public string Title { get; }
}