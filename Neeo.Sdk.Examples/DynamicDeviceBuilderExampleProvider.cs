using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Examples;

public sealed class DynamicDeviceBuilderExampleProvider(
    ILogger<DynamicDeviceBuilderExampleProvider> logger
) : IDeviceProvider
{
    private readonly DeviceInfo[] _dummyDevices = [
        new("unique-device-id-001", "PRO: 1st device", "I live in ROOM A", IsPro: true),
        new("unique-device-id-002", "PRO: 2nd device, unreachable", "I live in ROOM A", IsPro: true, IsReachable: false),
        new("unique-device-id-003", "STANDARD: 3rd device, unreachable", "I live in ROOM B", IsPro: false, IsReachable: false),
        new("unique-device-id-004", "STANDARD: 4th device", "I live in ROOM B", IsPro: false),
    ];
    private double _sliderValue = 0d;
    private bool _switchValue = true;

    public IDeviceBuilder DeviceBuilder => field ??= this.CreateDevice();

    private IDeviceBuilder BuildProDevice() => Device.Create(Constants.DeviceName, DeviceType.Light)
        .SetSpecificName("PRO Light")
        .AddCharacteristic(DeviceCharacteristic.DynamicDevice)
        .AddSwitch(Constants.PowerSwitch, null, this.GetSwitchValue, this.SetSwitchValue)
        .AddSlider(Constants.DimmerName, null, this.GetSliderValue, this.SetSliderValue);

    private IDeviceBuilder BuildStandardDevice() => Device.Create(Constants.DeviceName, DeviceType.Light)
        .SetSpecificName("STANDARD Light")
        .AddCharacteristic(DeviceCharacteristic.DynamicDevice)
        .AddSwitch(Constants.PowerSwitch, null, this.GetSwitchValue, this.SetSwitchValue);

    private IDeviceBuilder CreateDevice() => Device.Create(Constants.DeviceName, DeviceType.Accessory)
        .SetSpecificName(Constants.DeviceName)
        .AddCharacteristic(DeviceCharacteristic.AddAnotherDevice)
        .AddCharacteristic(DeviceCharacteristic.BridgeDevice)
        .EnableDiscovery(
            Constants.DeviceName,
            "This example shows how devices can share code. The next screen will discover some example light devices.",
            this.DiscoverAsync,
            enableDynamicDeviceBuilder: true
        )
        .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.NotifyDeviceList)
        .EnableRegistration("SDK Example", "SDK Dynamic Device Builder Example", (_) => Task.FromResult(true), (_, _) => Task.FromResult(RegistrationResult.Success));

    private DiscoveredDevice CreateDiscoveredDevice(DeviceInfo info)
    {
        (string id, string name, string room, bool isPro, bool? isReachable) = info;
        return new DiscoveredDevice(id, name, isReachable, room, isPro ? this.BuildProDevice() : this.BuildStandardDevice());
    }

    private Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        DeviceInfo[] devices = this.GetDeviceInfos(optionalDeviceId);
        return Task.FromResult(Array.ConvertAll(devices, this.CreateDiscoveredDevice));
    }

    private DeviceInfo[] GetDeviceInfos(string? optionalDeviceId)
    {
        return optionalDeviceId == null
            ? this._dummyDevices
            : Array.FindAll(this._dummyDevices, device => device.Id == optionalDeviceId);
    }

    private double GetSliderValue(string deviceId)
    {
        logger.LogInformation("GET SLIDER VALUE: {deviceId}", deviceId);
        return this._sliderValue;
    }

    private bool GetSwitchValue(string deviceId)
    {
        logger.LogInformation("GET SWITCH VALUE: {deviceId}", deviceId);
        return this._switchValue;
    }

    private Task NotifyDeviceList(string[] deviceIds, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task OnDeviceAdded(string deviceId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task OnDeviceRemoved(string deviceId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    private void SetSliderValue(string deviceId, double value)
    {
        logger.LogInformation("SET SLIDER VALUE: {deviceId}", deviceId);
        this._sliderValue = value;
    }

    private void SetSwitchValue(string deviceId, bool value)
    {
        logger.LogInformation("SET SWITCH VALUE: {deviceId}", deviceId);
        this._switchValue = value;
    }

    private readonly record struct DeviceInfo(
        string Id,
        string Name,
        string Room,
        bool IsPro,
        bool? IsReachable = null
    );

    private static class Constants
    {
        public const string DeviceName = "SDK Dynamic Device Builder Example";
        public const string DimmerName = "brightness-slider";
        public const string PowerSwitch = "power-switch";
    }
}
