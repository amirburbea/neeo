using System;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// A list info item which when clicked displays a dialog.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ListInfoItem"/> class.
/// </remarks>
/// <param name="title">The title of the info item.</param>
/// <param name="text">The text of the info item dialog.</param>
/// <param name="actionIdentifier">The (optional) action identifier to be performed upon clicking this item, typically used to point to a file to open.</param>
/// <param name="affirmativeButtonText"></param>
/// <param name="negativeButtonText"></param>
/// <remarks>
/// While these functions work in the NEEO application and EUI page, <paramref name="affirmativeButtonText"/>
/// and <paramref name="negativeButtonText"/> don't work on the remote, and should therefore be avoided.
/// </remarks>
public sealed class ListInfoItem(
    string title,
    string text,
    string? actionIdentifier = default,
    string? affirmativeButtonText = default,
    string? negativeButtonText = default
    ) : ClickableListItemBase(actionIdentifier)
{
    /// <summary>
    /// Within the dialog,
    /// </summary>
    public string? AffirmativeButtonText { get; } = affirmativeButtonText;

    /// <summary>
    /// Tells the NEEO Brain that this is an info item.
    /// </summary>
    public bool IsInfoItem { get; } = true;

    /// <summary>
    /// Within the dialog,
    /// </summary>
    public string? NegativeButtonText { get; } = negativeButtonText;

    /// <summary>
    /// Gets the text of the info item dialog.
    /// </summary>
    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>
    /// Gets the title of the info item.
    /// </summary>
    public string Title { get; } = title ?? throw new ArgumentNullException(nameof(text));
}
