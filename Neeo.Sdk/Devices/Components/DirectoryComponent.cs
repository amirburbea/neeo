namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a directory component.
/// </summary>
public interface IDirectoryComponent : IComponent
{
    /// <summary>
    /// Gets the (optional) directory role.
    /// </summary>
    DirectoryRole? Role { get; }
}

internal sealed record DirectoryComponent(
    string Name,
    string Label,
    string Path,
    DirectoryRole? Role
) : Component(ComponentType.Directory, Name, Label, Path), IDirectoryComponent;