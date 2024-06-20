using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// User interface actions for standardized actions in a directory.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<DirectoryUIAction>))]
public enum DirectoryUIAction
{
    /// <summary>
    /// Close the directory browser.
    /// </summary>
    [Text("close")]
    Close,

    /// <summary>
    /// Return to the directory root.
    /// </summary>
    [Text("goToRoot")]
    GoToRoot,

    /// <summary>
    /// Go back one level in the directory hierarchy.
    /// </summary>
    [Text("goBack")]
    GoBack,

    /// <summary>
    /// Reload the current directory (this can also be accomplished by pulling down from the top).
    /// </summary>
    [Text("reload")]
    Reload
}
