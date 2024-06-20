using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Examples.Devices;

public sealed class DynamicDeviceBuilderExampleProvider(ILogger<DynamicDeviceBuilderExampleProvider> logger) : IDeviceProvider
{
    private readonly DeviceInfo[] _dummyDevices = [
        new("unique-device-id-001","PRO: 1st device","I live in ROOM A",IsPro: true),
        new("unique-device-id-002","PRO: 2nd device, unreachable","I live in ROOM A",IsPro:true,IsReachable:false),
        new("unique-device-id-003","STANDARD: 3rd device, unreachable","I live in ROOM B",IsPro:false,IsReachable:false),
        new("unique-device-id-004","STANDARD: 4th device, unreachable","I live in ROOM B",IsPro:false,IsReachable:true),
    ];

    private IDeviceBuilder? _deviceBuilder;
    private double _sliderValue = 0d;
    private bool _switchValue = true;

    public IDeviceBuilder DeviceBuilder => _deviceBuilder ??= this.CreateDevice();

    private IDeviceBuilder CreateDevice()
    {
        return Device.Create(Constants.DeviceName, DeviceType.Light)
            .SetSpecificName(Constants.DeviceName)
            .EnableDiscovery(Constants.DeviceName, "This SDK example shows how devices can share code the next screen will discover some of the light devices", this.DiscoverAsync, enableDynamicDeviceBuilder: true);
    }

    private async Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        DeviceInfo[] devices = this.GetDeviceInfos(optionalDeviceId);
        return Array.ConvertAll(devices, this.CreateDiscoveredDevice);
    }

    private DiscoveredDevice CreateDiscoveredDevice(DeviceInfo info)
    {
        (string id, string name, string room, bool isPro, bool? isReachable) = info;
        return new DiscoveredDevice(id, name, isReachable, room, isPro ? this.BuildProDevice() : this.BuildStandardDevice());
    }

    private IDeviceBuilder BuildProDevice()
    {
        return Device.Create(Constants.DeviceName, DeviceType.Light)
            .SetSpecificName("PRO Light")
            .AddCharacteristic(DeviceCharacteristic.DynamicDevice)
            .AddSwitch(Constants.PowerSwitch, null, this.GetSwitchValueAsync, this.SetSwitchValueAsync)
            .AddSlider(Constants.DimmerName, null, this.GetSliderValueAsync, this.SetSliderValueAsync);
    }

    private IDeviceBuilder BuildStandardDevice()
    {
        return Device.Create(Constants.DeviceName, DeviceType.Light)
            .SetSpecificName("STANDARD Light")
            .AddCharacteristic(DeviceCharacteristic.DynamicDevice)
            .AddSwitch(Constants.PowerSwitch, null, this.GetSwitchValueAsync, this.SetSwitchValueAsync);
    }

    private DeviceInfo[] GetDeviceInfos(string? optionalDeviceId)
    {
        return optionalDeviceId == null
            ? this._dummyDevices
            : Array.FindAll(this._dummyDevices, device => device.Id == optionalDeviceId);
    }

    private Task<double> GetSliderValueAsync(string deviceId, CancellationToken cancellationToken)
    {
        logger.LogInformation("GET SLIDER VALUE: {deviceId}", deviceId);
        return Task.FromResult(this._sliderValue);
    }

    private Task<bool> GetSwitchValueAsync(string deviceId, CancellationToken cancellationToken)
    {
        logger.LogInformation("GET SWITCH VALUE: {deviceId}", deviceId);
        return Task.FromResult(this._switchValue);
    }

    private Task SetSliderValueAsync(string deviceId, double value, CancellationToken cancellationToken)
    {
        logger.LogInformation("SET SLIDER VALUE: {deviceId}", deviceId);
        this._sliderValue = value;
        return Task.CompletedTask;
    }

    private Task SetSwitchValueAsync(string deviceId, bool value, CancellationToken cancellationToken)
    {
        logger.LogInformation("SET SWITCH VALUE: {deviceId}", deviceId);
        this._switchValue = value;
        return Task.CompletedTask;
    }

    private readonly record struct DeviceInfo(string Id, string Name, string Room, bool IsPro, bool? IsReachable = null);

    private static class Constants
    {
        public const string DeviceName = "Dynamic Device Builder Example";
        public const string DimmerName = "brightness-slider";
        public const string PowerSwitch = "power-switch";
    }
}
