using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/discover")]
    public async Task<ActionResult<object>> DiscoverAsync(string adapterName, CancellationToken cancellationToken)
    {
        if (await this.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter || adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature feature)
        {
            return this.NotFound();
        }
        logger.LogInformation("Performing discovery for {adapter}...", adapter.DeviceName);
        DiscoveredDevice[] devices = await feature.DiscoverAsync(cancellationToken);
        if (devices.Length == 0 || !feature.EnableDynamicDeviceBuilder)
        {
            return devices;
        }
        DynamicDiscoveredDevice[] dynamicDevices = new DynamicDiscoveredDevice[devices.Length];
        for (int index = 0; index < devices.Length; index++)
        {
            (string id, string name, bool? reachable, string? room, IDeviceBuilder? builder) = devices[index];
            IDeviceAdapter deviceAdapter = dynamicDeviceRegistry.RegisterDiscoveredDevice(adapter, id, builder!);
            dynamicDevices[index] = new(id, name, reachable, room, new(deviceAdapter));
        }
        return dynamicDevices;
    }

    public readonly record struct DynamicDiscoveredDevice(
        string Id,
        string Name,
        bool? Reachable,
        string? Room,
        DynamicDevice Device
    );

    public readonly struct DynamicDevice(IDeviceAdapter adapter)
    {
        public string AdapterName { get; } = adapter.AdapterName;

        [JsonPropertyName("apiversion")]
        public string ApiVersion { get; } = "1.0";

        [JsonPropertyName("capabilities")]
        public IReadOnlyCollection<IComponent> Components { get; } = adapter.Components;

        public IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; } = adapter.DeviceCapabilities;

        public int? DriverVersion { get; } = adapter.DriverVersion;

        public string Manufacturer { get; } = adapter.Manufacturer;

        public DeviceSetup Setup { get; } = adapter.Setup;

        public DeviceTiming? Timing { get; } = adapter.Timing;

        public DeviceType Type { get; } = adapter.Type;
    }
}
