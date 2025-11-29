namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a directory component.
/// </summary>
public sealed record DirectoryComponent(
    string Name,
    string Label,
    string Path,
    DirectoryRole? Role
) : Component(ComponentType.Directory, Name, Label, Path);
