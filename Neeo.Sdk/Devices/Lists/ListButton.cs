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

public sealed class ListButton
{
    public ListButton(string title, string actionIdentifier, bool? inverse = default, ListButtonIcon? icon = default, ListUIAction? uiAction = default)
    {
        this.Title = Validator.ValidateText(title, maxLength: 255);
        this.Icon = icon;
        this.Inverse = inverse;
        this.ActionIdentifier = actionIdentifier;
        this.UIAction = uiAction;
    }

    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; }

    public string ActionIdentifier { get; }

    [JsonPropertyName("iconName")]
    public ListButtonIcon? Icon { get; }

    public bool? Inverse { get; }

    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    public bool IsButton { get; } = true;

    public string Title { get; }
}
