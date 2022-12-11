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
public sealed class ListButton : ClickableListItemBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="title">The title of the button.</param>
    /// <param name="actionIdentifier"></param>
    /// <param name="inverse"></param>
    /// <param name="icon"></param>
    /// <param name="uiAction"></param>
    public ListButton(
        string title,
        string actionIdentifier,
        bool? inverse = default,
        ListButtonIcon? icon = default,
        ListUIAction? uiAction = default
    ) : base(actionIdentifier, uiAction)
    {
        this.Title = Validator.ValidateText(title, maxLength: 255);
        this.Icon = icon;
        this.Inverse = inverse;
    }

    [JsonPropertyName("iconName")]
    public ListButtonIcon? Icon { get; }

    public bool? Inverse { get; }

    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    public bool IsButton { get; } = true;

    /// <summary>
    /// Gets the title of the button.
    /// </summary>
    public string Title { get; }
}