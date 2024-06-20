using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

[JsonConverter(typeof(TextJsonConverter<DirectoryButtonIcon>))]
public enum DirectoryButtonIcon
{
    [Text("shuffle")]
    Shuffle,

    [Text("repeat")]
    Repeat
}

public sealed class DirectoryButton(
    string title, 
    string actionIdentifier, 
    bool? inverse = default, 
    DirectoryButtonIcon? icon = default, 
    DirectoryUIAction? uiAction = default
) : ClickableDirectoryItemBase(actionIdentifier, uiAction)
{
    [JsonPropertyName("iconName")]
    public DirectoryButtonIcon? Icon { get; } = icon;

    public bool? Inverse { get; } = inverse;

    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    public bool IsButton { get; } = true;

    public string Title { get; } = Validator.ValidateText(title, maxLength: 255);
}
