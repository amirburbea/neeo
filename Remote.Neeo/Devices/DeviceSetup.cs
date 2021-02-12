using System.Text.Json.Serialization;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
    public interface IDeviceSetup
    {
        [JsonPropertyName("introtext")]
        string? Description { get; }

        bool? Discovery { get; }

        bool? EnableDynamicDeviceBuilder { get; }

        [JsonPropertyName("introheader")]
        string? HeaderText { get; }

        bool? Registration { get; }

        RegistrationType? RegistrationType { get; }
    }

    internal sealed class DeviceSetup : IDeviceSetup
    {
        public string? Description { get; set; }

        public bool? Discovery { get; set; }

        public bool? EnableDynamicDeviceBuilder { get; set; }

        public string? HeaderText { get; set; }

        public bool? Registration => this.RegistrationType.HasValue ? true : default(bool?);

        public RegistrationType? RegistrationType { get; set; }
    }
}
