using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Base class for a clickable list item.
/// </summary>
/// <param name="actionIdentifier">The (optional) action identifier to be performed upon clicking this item, typically used to point to a file to open.</param>
/// <param name="uiAction">The (optional) list UI action - special predefined actions that can be performed upon clicking this item.</param>
public abstract class ClickableListItemBase(string? actionIdentifier = default, ListUIAction? uiAction = default)
{

    /// <summary>
    /// Gets the (optional) identifier of the action to be performed upon clicking this item, typically used to point to a file to open.
    /// </summary>
    public string? ActionIdentifier { get; } = actionIdentifier;

    /// <summary>
    /// Gets the (optional) list UI action - special predefined actions that can be performed upon clicking this item.
    /// </summary>
    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; } = uiAction;
}