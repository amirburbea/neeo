using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// User interface actions for buttons/items in the list.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<ListUIAction>))]
public enum ListUIAction
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
    /// Reload the current list (although this can be accomplished by pulling down from the top).
    /// </summary>
    [Text("reload")]
    Reload
}
