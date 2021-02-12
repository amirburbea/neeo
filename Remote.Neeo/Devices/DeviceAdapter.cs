using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    public interface IDeviceAdapter
    {
        string AdapterName { get; }

        [JsonPropertyName("apiverson")]
        string ApiVersion => "1.0";

        IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        DeviceTiming Timing { get; }

        IReadOnlyCollection<DeviceInfo> Devices { get; }

        uint? DriverVersion { get; }

        DeviceInitializer? Initializer { get; }

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
            DeviceTiming? timing,
            IReadOnlyCollection<string> tokens,
            string? specificName,
            DeviceIconOverride? icon,
            IReadOnlyCollection<DeviceCapability> capabilities,
            IDeviceSetup setup,
            DeviceInitializer? initializer
        )
        {
            this.AdapterName = adapterName;
            this.Type = type;
            this.Manufacturer = manufacturer;
            this.Initializer = initializer;
            this.Timing = timing ?? DeviceTiming.Empty;
            this.Capabilities = capabilities;
            this.DriverVersion = driverVersion;
            this.Setup = setup;
            this.Devices = new[] { new DeviceInfo(deviceName, tokens, specificName, icon) };
        }

        public string AdapterName { get; }

        public IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        public DeviceTiming Timing { get; }

        public IReadOnlyCollection<DeviceInfo> Devices { get; }

        public uint? DriverVersion { get; }

        public DeviceInitializer? Initializer { get; }

        public string Manufacturer { get; }

        public IDeviceSetup Setup { get; }

        public DeviceType Type { get; }
    }
}
