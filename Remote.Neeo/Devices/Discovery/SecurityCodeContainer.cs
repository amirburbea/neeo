using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Discovery
{
    internal readonly struct SecurityCodeContainer
    {
        [JsonConstructor]
        public SecurityCodeContainer(string securityCode) => this.SecurityCode = securityCode;

        public string SecurityCode { get; }
    }
}