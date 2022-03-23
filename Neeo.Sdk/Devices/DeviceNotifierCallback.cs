namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked when the server is started, providing an object that can be used to
/// send notifications about the state of the device.
/// </summary>
/// <param name="notifier">An object which sends notifications about device state to the NEEO Brain.</param>
public delegate void DeviceNotifierCallback(IDeviceNotifier notifier);