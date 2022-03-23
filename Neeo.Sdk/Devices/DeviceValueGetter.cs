using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously get a value from a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to get from the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<TValue> DeviceValueGetter<TValue>(string deviceId)
    where TValue : notnull;