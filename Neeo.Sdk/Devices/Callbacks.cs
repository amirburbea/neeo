using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;



/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously get a value from a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to get from the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<TValue> DeviceValueGetter<TValue>(string deviceId)
    where TValue : notnull;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously set a value on a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to set on the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="value">The value to set.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task DeviceValueSetter<TValue>(string deviceId, TValue value)
    where TValue : notnull;

