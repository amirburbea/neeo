using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Base class for a clickable directory item.
/// </summary>
/// <param name="actionIdentifier">The (optional) action identifier.</param>
/// <param name="uiAction">The (optional) standardized directory UI action.</param>
public abstract class ClickableDirectoryItemBase(
    string? actionIdentifier = default,
    DirectoryUIAction? uiAction = default
)
{
    /// <summary>
    /// Gets the optional action identifier.
    /// </summary>
    public string? ActionIdentifier => actionIdentifier;

    /// <summary>
    /// Gets the optional standardized directory action.
    /// </summary>
    [JsonPropertyName("uiAction")]
    public DirectoryUIAction? UIAction => uiAction;
}
