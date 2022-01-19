namespace Neeo.Api.Devices;

public interface IPowerCallbacks
{
    DeviceAction? PowerOffNotification { get; }

    DeviceAction? PowerOnNotification { get; }
}

internal record class PowerCallbacks(
    DeviceAction? PowerOnNotification = default,
    DeviceAction? PowerOffNotification = default
) : IPowerCallbacks;