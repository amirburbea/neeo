using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// The optional role associated with a directory.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<DirectoryRole>))]
public enum DirectoryRole
{
    /// <summary>
    /// The general purpose directory for browsing media options.
    /// </summary>
    [Text("ROOT")]
    Root = 1,

    /// <summary>
    /// The queue - typically only used with the player widget.
    /// </summary>
    [Text("QUEUE")]
    Queue = 2
}
