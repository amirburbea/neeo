using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously notify that a device
/// has been added or removed from the system.
/// </summary>
/// <param name="deviceId">The identifier of the device.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task DeviceSubscriptionHandler(string deviceId);
