using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    public interface IDeviceInfo
    {
        DeviceIconOverride? Icon { get; }

        string Name { get; }

        [JsonPropertyName("specificname")]
        string? SpecificName { get; }

        IReadOnlyCollection<string> Tokens { get; }
    }

    internal sealed class DeviceInfo : IDeviceInfo
    {
        public DeviceInfo(string name, IReadOnlyCollection<string> tokens, string? specificName, DeviceIconOverride? icon)
        {
            (this.Name, this.Tokens, this.SpecificName, this.Icon) = (name, tokens, specificName, icon);
        }

        public DeviceIconOverride? Icon { get; }

        public string Name { get; }

        public string? SpecificName { get; }

        public IReadOnlyCollection<string> Tokens { get; }
    }
}
