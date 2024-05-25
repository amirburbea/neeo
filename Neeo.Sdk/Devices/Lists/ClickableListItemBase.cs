using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Base class for a clickable list item.
/// </summary>
/// <remarks>
///
/// </remarks>
/// <param name="actionIdentifier">The (optional) action identifier.</param>
/// <param name="uiAction">The (optional) action list UI action.</param>
public abstract class ClickableListItemBase(string? actionIdentifier = default, ListUIAction? uiAction = default)
{
    public string? ActionIdentifier { get; } = actionIdentifier;

    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; } = uiAction;
}
