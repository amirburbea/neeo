namespace Remote.Neeo.Devices.Components;

public interface IButtonComponent : IComponent
{
}

internal sealed class ButtonComponent : Component, IButtonComponent
{
    public ButtonComponent(string name, string? label, string pathPrefix)
        : base(ComponentType.Button, name, label, pathPrefix)
    {
    }
}
