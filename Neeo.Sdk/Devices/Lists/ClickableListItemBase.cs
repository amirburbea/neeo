using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Base class for a clickable list item.
/// </summary>
public abstract class ClickableListItemBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionIdentifier">The (optional) action identifier.</param>
    /// <param name="uiAction">The (optional) action list UI action.</param>
    protected ClickableListItemBase(string? actionIdentifier = default, ListUIAction? uiAction = default)
    {
        this.ActionIdentifier = actionIdentifier;
        this.UIAction = uiAction;
    }

    public string? ActionIdentifier { get; }

    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; }
}