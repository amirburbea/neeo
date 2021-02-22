using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IDeviceAdapter
    {
        string AdapterName { get; }

        /// <summary>
        /// The API version is always &quot;1.0&quot;.
        /// </summary>
        [JsonPropertyName("apiverson"), EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        string ApiVersion => "1.0";

        IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        IReadOnlyCollection<DeviceInfo> Devices { get; }

        uint? DriverVersion { get; }

        DeviceInitializer? Initializer { get; }

        string Manufacturer { get; }

        IDeviceSetup Setup { get; }

        DeviceTiming Timing { get; }

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

        public IReadOnlyCollection<DeviceInfo> Devices { get; }

        public uint? DriverVersion { get; }

        public DeviceInitializer? Initializer { get; }

        public string Manufacturer { get; }

        public IDeviceSetup Setup { get; }

        public DeviceTiming Timing { get; }

        public DeviceType Type { get; }
    }
}