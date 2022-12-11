using System;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListInfoItem : ClickableListItemBase
{
    public ListInfoItem(
        string title,
        string text,
        string? actionIdentifier = default
    ) : base(actionIdentifier)
    {
        this.Title = title ?? throw new ArgumentNullException(nameof(text));
        this.Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListInfoItem"/> class.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="text"></param>
    /// <param name="actionIdentifier"></param>
    /// <param name="affirmativeButtonText"></param>
    /// <param name="negativeButtonText"></param>
    /// <remarks>
    /// While these functions work in the NEEO application and browser EUI page,
    /// <paramref name="affirmativeButtonText"/> and <paramref name="negativeButtonText"/> don't work on the Remote
    /// and should therefore be avoided.
    /// </remarks>
    [Obsolete("While these functions work in the NEEO application and browser EUI page, affirmativeButtonText and negativeButtonText don't work on the Remote")]
    public ListInfoItem(
        string title,
        string text,
        string? actionIdentifier = default,
        string? affirmativeButtonText = default,
        string? negativeButtonText = default
    ) : this(title, text, actionIdentifier)
    {
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
}