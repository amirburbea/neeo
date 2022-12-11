using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Base class for a clickable list item.
/// </summary>
public abstract class ClickableListItemBase
{
    /// <summary>
    /// Initializes a new <see cref="ClickableListItemBase"/> instance.
    /// </summary>
    /// <param name="actionIdentifier">The (optional) action identifier to be performed upon clicking this item, typically used to point to a file to open.</param>
    /// <param name="uiAction">The (optional) list UI action - special predefined actions that can be performed upon clicking this item.</param>
    protected ClickableListItemBase(string? actionIdentifier = default, ListUIAction? uiAction = default)
    {
        this.ActionIdentifier = actionIdentifier;
        this.UIAction = uiAction;
    }

    /// <summary>
    /// Gets the (optional) identifier of the action to be performed upon clicking this item, typically used to point to a file to open.
    /// </summary>
    public string? ActionIdentifier { get; }

    /// <summary>
    /// Gets the (optional) list UI action - special predefined actions that can be performed upon clicking this item.
    /// </summary>
    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; }
}