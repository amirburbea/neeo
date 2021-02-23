using Remote.Neeo.Json;

namespace Remote.Neeo.Devices.Features
{
    [JsonInterfaceConverter(typeof(IFeature))]
    public interface IFeature
    {
        ComponentType ComponentType { get; }

        string? Label { get; }

        string Name { get; }
    }
}