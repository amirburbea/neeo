namespace Neeo.Sdk.Devices.Components;

public interface IDirectoryComponent : IComponent
{
    string Identifier { get; }

    DirectoryRole? Role { get; }
}

internal sealed record DirectoryComponent(
    string Name,
    string Label,
    string Path,
    string Identifier,
    DirectoryRole? Role
) : Component(ComponentType.Directory, Name, Label, Path), IDirectoryComponent;