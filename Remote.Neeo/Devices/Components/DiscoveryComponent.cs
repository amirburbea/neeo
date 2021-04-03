namespace Remote.Neeo.Devices.Components
{
    public interface IDiscoveryComponent : IComponent
    {
    }

    internal sealed class DiscoveryComponent : Component, IDiscoveryComponent
    {
        public DiscoveryComponent(string pathPrefix)
            : base(ComponentType.Discovery, TextAttribute.GetEnumText(ComponentType.Discovery), default, pathPrefix)
        {
        }
    }
}