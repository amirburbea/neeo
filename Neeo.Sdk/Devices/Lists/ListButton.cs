using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

[JsonConverter(typeof(TextJsonConverter<ListButtonIcon>))]
public enum ListButtonIcon
{
    [Text("shuffle")]
    Shuffle,

    [Text("repeat")]
    Repeat
}

/// <summary>
/// 
/// </summary>
/// <remarks>
/// 
/// </remarks>
/// <param name="title">The title of the button.</param>
/// <param name="actionIdentifier"></param>
/// <param name="inverse"></param>
/// <param name="icon"></param>
/// <param name="uiAction"></param>
public sealed class ListButton(
    string title,
    string actionIdentifier,
    bool? inverse = default,
    ListButtonIcon? icon = default,
    ListUIAction? uiAction = default
) : ClickableListItemBase(actionIdentifier, uiAction)
{
    [JsonPropertyName("iconName")]
    public ListButtonIcon? Icon { get; } = icon;

    public bool? Inverse { get; } = inverse;

    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    public bool IsButton { get; } = true;

    /// <summary>
    /// Gets the title of the button.
    /// </summary>
    public string Title { get; } = Validator.ValidateText(title, maxLength: 255);
}