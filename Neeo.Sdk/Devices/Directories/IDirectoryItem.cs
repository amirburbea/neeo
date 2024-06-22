using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Interface for a directory item.
/// </summary>
[JsonDirectSerialization<IDirectoryItem>]
public interface IDirectoryItem
{
    /// <summary>
    /// Gets the type of the directory item.
    /// </summary>
    DirectoryItemType Type { get; }
}
