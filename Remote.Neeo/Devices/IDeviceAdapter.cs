using System.Collections.Generic;
using System.Text.Json.Serialization;
using Remote.Neeo.Devices.Components;

namespace Remote.Neeo.Devices;

public interface IDeviceAdapter
{
    string AdapterName { get; }

    /// <summary>
    /// The API version is always &quot;1.0&quot;.
    /// </summary>
    [JsonPropertyName("apiverson")]
    string ApiVersion => "1.0";

    IReadOnlyCollection<IComponent> Capabilities { get; }

    [JsonPropertyName("deviceCapabilities")]
    IReadOnlyCollection<Characteristic> Characteristics { get; }

    IReadOnlyCollection<IDeviceInfo> Devices { get; }

    uint? DriverVersion { get; }

    [JsonPropertyName("handler")]
    IReadOnlyDictionary<string, ICapabilityHandler> Handlers { get; }

    ICapabilityHandler? GetHandler(ComponentType type)
    {
        string text = TextAttribute.GetEnumText(type);
        return this.Handlers.TryGetValue(text, out ICapabilityHandler? handler) ? handler : default;
    }

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
        DeviceType type,
        string manufacturer,
        uint? driverVersion,
        DeviceTiming? timing,
        IReadOnlyCollection<Characteristic> characteristics,
        IDeviceSetup setup,
        DeviceInitializer? initializer,
        string deviceName,
        IReadOnlyCollection<string> tokens,
        string? specificName,
        DeviceIconOverride? icon,
        IReadOnlyCollection<IComponent> capabilities,
        IReadOnlyDictionary<string, ICapabilityHandler> handlers)
    {
        this.AdapterName = adapterName;
        this.Type = type;
        this.Manufacturer = manufacturer;
        this.Initializer = initializer;
        this.Timing = timing ?? new();
        this.Characteristics = characteristics;
        this.DriverVersion = driverVersion;
        this.Setup = setup;
        this.Devices = new[] { new DeviceInfo(deviceName, tokens, specificName, icon) };
        this.Capabilities = capabilities;
        this.Handlers = handlers;
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<IComponent> Capabilities { get; }

    public IReadOnlyCollection<Characteristic> Characteristics { get; }

    public IReadOnlyCollection<IDeviceInfo> Devices { get; }

    public uint? DriverVersion { get; }

    public IReadOnlyDictionary<string, ICapabilityHandler> Handlers { get; }

    public DeviceInitializer? Initializer { get; }

    public string Manufacturer { get; }

    public IDeviceSetup Setup { get; }

    public DeviceTiming Timing { get; }

    public DeviceType Type { get; }

    private sealed class DeviceInfo : IDeviceInfo
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
