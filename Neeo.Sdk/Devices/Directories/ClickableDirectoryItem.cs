using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Base class for a clickable directory item (wire representation).
/// </summary>
/// <param name="ActionIdentifier">The (optional) action identifier.</param>
/// <param name="UIAction">The (optional) standardized directory UI action.</param>
internal abstract record ClickableDirectoryItem(
    string? ActionIdentifier = null,
    [property: JsonPropertyName("uiAction")] DirectoryUIAction? UIAction = null
);
