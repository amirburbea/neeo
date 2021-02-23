using Remote.Neeo.Json;

namespace Remote.Neeo.Devices.Features
{
    [JsonInterfaceConverter(typeof(ISensorFeature))]
    public interface ISensorFeature : IFeature
    {
        double RangeLow { get; }

        double RangeHigh { get; }

        string Units { get; }
    }
}