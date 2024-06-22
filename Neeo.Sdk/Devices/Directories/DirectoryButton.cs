using System.ComponentModel;
using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Defines the standard directory button icons.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<DirectoryButtonIcon>))]
public enum DirectoryButtonIcon
{
    /// <summary>
    /// Play shuffle.
    /// </summary>
    [Text("shuffle")]
    Shuffle,

    /// <summary>
    /// Repeat play.
    /// </summary>
    [Text("repeat")]
    Repeat
}

/// <summary>
/// Defines a button in a directory.
/// </summary>
/// <param name="Text">The text for the button.</param>
/// <param name="Icon">Optional, standard button icon (only for Repeat and Shuffle).</param>
/// <param name="Inverse">Optional, set to <c>true</c> to have the button appear in an inverse color scheme.</param>
/// <param name="ActionIdentifier">The (optional) action identifier.</param>
/// <param name="UIAction">The (optional) standardized directory UI action.</param>
public sealed record class DirectoryButton(
    string Text,
    [property: JsonPropertyName("iconName")] DirectoryButtonIcon? Icon = default,
    bool? Inverse = null,
    string? ActionIdentifier = null,
    DirectoryUIAction? UIAction = null
) : ClickableDirectoryItem(ActionIdentifier, UIAction)
{
    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsButton { get; } = true;
}
