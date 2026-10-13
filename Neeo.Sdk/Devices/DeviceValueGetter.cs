using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously get a value from a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to get from the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<TValue> AsyncDeviceValueGetter<TValue>(string deviceId, CancellationToken cancellationToken = default)
    where TValue : notnull;

/// <summary>
/// Callback invoked by the NEEO Brain to synchronously get a value from a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to get from the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <returns><typeparamref name="TValue"/> result.</returns>
public delegate TValue DeviceValueGetter<TValue>(string deviceId)
    where TValue : notnull;

internal static class DeviceValueGetter
{
    public static AsyncDeviceValueGetter<TValue> AsAsync<TValue>(this DeviceValueGetter<TValue> getter)
        where TValue : notnull
    {
        return (deviceId, _) => Task.FromResult(getter(deviceId));
    }
}
