namespace Neeo.Api.Devices;

public interface IPowerNotifications
{
    DeviceAction NotifyPowerOff { get; }

    DeviceAction NotifyPowerOn { get; }

    void Deconstruct(out DeviceAction notifyPowerOn, out DeviceAction notifyPowerOff);
}

internal sealed record class PowerNotifications(
    DeviceAction NotifyPowerOn,
    DeviceAction NotifyPowerOff
) : IPowerNotifications;