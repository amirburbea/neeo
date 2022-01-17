namespace Neeo.Api.Devices.Components;

public interface IDiscoveryComponent : IComponent
{
}

internal sealed class DiscoveryComponent : Component, IDiscoveryComponent
{
    public DiscoveryComponent(string pathPrefix)
        : base(ComponentType.Discovery, TextAttribute.GetText(ComponentType.Discovery), default, pathPrefix)
    {
    }
}
