namespace Neeo.Sdk.Devices.Components;

public interface IDirectoryComponent : IComponent
{
    DirectoryRole? Role { get; }
}

internal sealed record DirectoryComponent(
    string Name,
    string Label,
    string Path,
    DirectoryRole? Role
) : Component(ComponentType.Directory, Name, Label, Path), IDirectoryComponent;