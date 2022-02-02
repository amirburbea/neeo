﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

internal class ConsoleExampleDevice : IExampleDevice
{
    private readonly ILogger<ConsoleExampleDevice> _logger;

    public ConsoleExampleDevice(ILogger<ConsoleExampleDevice> logger)
    {
        this._logger = logger;
        const string deviceName = "Console Example Device";
        this.Builder = Device.Create(deviceName, DeviceType.TV)
            .SetSpecificName(deviceName)
            .SetManufacturer("NEEO")
            .SetIcon(DeviceIconOverride.NeeoBrain)
            .AddButtonGroup(ButtonGroups.Power)
            .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.ChannelZapper | ButtonGroups.Volume | ButtonGroups.MenuAndBack)
            .AddButton(KnownButtons.InputHdmi1)
            .AddButton(KnownButtons.InputHdmi2 | KnownButtons.InputHdmi3)
            .RegisterDeviceSubscriptionCallbacks(this.OnDeviceAdded, this.OnDeviceRemoved, this.InitializeDeviceList)
            .AddButtonHandler(this.OnButtonPressed);
    }

    public IDeviceBuilder Builder { get; }

    private Task InitializeDeviceList(string[] deviceIds)
    {
        this._logger.LogInformation("Initialized with [{deviceIds}]", string.Join(',', deviceIds));
        return Task.CompletedTask;
    }

    private Task OnButtonPressed(string deviceId, string button)
    {
        this._logger.LogInformation("Button {button} pressed on device: {deviceId}.", button, deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceAdded(string deviceId)
    {
        this._logger.LogInformation("Device added '{deviceId}'", deviceId);
        return Task.CompletedTask;
    }

    private Task OnDeviceRemoved(string deviceId)
    {
        this._logger.LogInformation("Device removed '{deviceId}'", deviceId);
        return Task.CompletedTask;
    }
}