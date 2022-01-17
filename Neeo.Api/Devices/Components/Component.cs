namespace Neeo.Api.Devices.Components;

public interface IComponent
{
    string Label { get; }

    string Name { get; }

    string Path { get; }

    ComponentType Type { get; }
}

internal abstract class Component : IComponent
{
    protected Component(ComponentType type, string name, string? label, string pathPrefix)
    {
        (this.Type, this.Name, this.Label, this.Path) = (type, name, label ?? name, pathPrefix + name);
    }

    public string Label { get; }

    public string Name { get; }

    public string Path { get; }

    public ComponentType Type { get; }

    protected string GetAssociatedSensorName() => $"{this.Name}_SENSOR";
}
