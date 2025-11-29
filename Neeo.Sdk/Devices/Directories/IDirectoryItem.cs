using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Interface for a directory item.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(DirectoryButtonRow))]
[JsonDerivedType(typeof(DirectoryEntry))]
[JsonDerivedType(typeof(DirectoryHeader))]
[JsonDerivedType(typeof(DirectoryInfoItem))]
[JsonDerivedType(typeof(DirectoryTileRow))]
public interface IDirectoryItem
{
    /// <summary>
    /// Gets the type of the directory item.
    /// </summary>
    [JsonIgnore]
    DirectoryItemType Type { get; }
}
