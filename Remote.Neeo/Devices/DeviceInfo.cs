using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    public readonly struct DeviceInfo
    {
        public DeviceInfo(string name, IReadOnlyCollection<string> tokens, string? specificName, DeviceIconOverride? icon)
        {
            (this.Name, this.Tokens, this.SpecificName, this.Icon) = (name, tokens, specificName, icon);
        }

        public DeviceIconOverride? Icon { get; }

        public string Name { get; }

        [JsonPropertyName("specificname")]
        public string? SpecificName { get; }

        public IReadOnlyCollection<string> Tokens { get; }
    }
}
