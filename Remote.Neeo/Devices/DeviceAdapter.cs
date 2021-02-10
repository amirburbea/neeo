using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    public interface IDeviceAdapter
    {
        string AdapterName { get; }

        string ApiVersion { get; }

        IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        DelaysSpecifier Delays { get; }

        IReadOnlyCollection<DeviceInfo> Devices { get; }

        uint? DriverVersion { get; }

        IDeviceInitializer? Initializer { get; }

        string Manufacturer { get; }

        IDeviceSetup Setup { get; }

        DeviceType Type { get; }
    }

    internal sealed class DeviceAdapter : IDeviceAdapter
    {
        public DeviceAdapter(
            string adapterName,
            string deviceName,
            DeviceType type,
            string manufacturer,
            uint? driverVersion,
            DelaysSpecifier? delays,
            IReadOnlyCollection<string> tokens,
            string? specificName,
            DeviceIcon? icon,
            IReadOnlyCollection<DeviceCapability> capabilities,
            IDeviceSetup setup,
            IDeviceInitializer? initializer
        )
        {
            this.AdapterName = adapterName;
            this.Type = type;
            this.Manufacturer = manufacturer;
            this.Initializer = initializer;
            this.Delays = delays ?? DelaysSpecifier.Empty;
            this.Capabilities = capabilities;
            this.DriverVersion = driverVersion;
            this.Setup = setup;
            this.Devices = new[] { new DeviceInfo(deviceName, tokens, specificName, icon) };
        }

        public string AdapterName { get; }

        [JsonPropertyName("apiverson")]
        public string ApiVersion => "1.0";

        public IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        [JsonPropertyName("timing")]
        public DelaysSpecifier Delays { get; }

        public IReadOnlyCollection<DeviceInfo> Devices { get; }

        public uint? DriverVersion { get; }

        public IDeviceInitializer? Initializer { get; }

        public string Manufacturer { get; }

        public IDeviceSetup Setup { get; }

        public DeviceType Type { get; }
    }
}
