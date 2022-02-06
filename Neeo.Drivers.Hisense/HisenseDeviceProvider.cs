using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseDeviceProvider : IDeviceProvider
{
    private readonly ILogger<HisenseDeviceProvider> _logger;
    private readonly string _settingsFilePath;

    public HisenseDeviceProvider(ILogger<HisenseDeviceProvider> logger)
    {
        this._logger = logger;
        this._settingsFilePath = Path.Combine(
            Environment.GetFolderPath(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Environment.SpecialFolder.LocalApplicationData : Environment.SpecialFolder.UserProfile
            ),
            $"{nameof(Hisense)}.json"
        );
    }

    public IDeviceBuilder ProvideDevice() => Device.Create(Constants.DriverName, DeviceType.Accessory)
        .SetSpecificName(Constants.DriverName)
        .SetManufacturer(Constants.Manufacturer)
        .AddAdditionalSearchTokens("TCP")
        .RegisterInitializer(this.InitializeAsync)
        .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync, enableDynamicDeviceBuilder: true);

    private async Task InitializeAsync()
    {
        if (NetworkDevices.GetNetworkDevices() is not { Count: > 0 } networkDevices)
        {
            return;
        }
    }

    private Task<DiscoveredDevice[]> PerformDiscoveryAsync(string? optionalDeviceId)
    {
        return Task.FromResult(Array.Empty<DiscoveredDevice>());
    }

    private async Task<DeviceTuple?> RestoreDevice(IReadOnlyDictionary<IPAddress, PhysicalAddress> networkDevices)
    {
        if (File.Exists(this._settingsFilePath))
        {
            try
            {
                using Stream stream = File.OpenRead(this._settingsFilePath);
                if (await JsonSerializer.DeserializeAsync<string[]>(stream, JsonSerialization.Options).ConfigureAwait(false) is { Length: > 0 } array)
                {
                    PhysicalAddress macAddress = PhysicalAddress.Parse(array[0]);
                    foreach ((IPAddress ipAddress, PhysicalAddress physicalAddress) in networkDevices)
                    {
                        if (physicalAddress.Equals(macAddress))
                        {
                            return new(ipAddress, physicalAddress);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore.
            }
        }
        return null;
    }

    private record struct DeviceTuple(IPAddress IPAddress, PhysicalAddress MacAddress);
}

internal static class Constants
{
    public const string DriverName = "IP Controlled TV";
    public const string Manufacturer = nameof(Hisense);
}