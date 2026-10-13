using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously set a value on a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to set on the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="value">The value to set.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task AsyncDeviceValueSetter<TValue>(string deviceId, TValue value, CancellationToken cancellationToken = default)
    where TValue : notnull;

/// <summary>
/// Callback invoked by the NEEO Brain to synchronously set a value on a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to set on the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="value">The value to set.</param>
public delegate void DeviceValueSetter<TValue>(string deviceId, TValue value)
    where TValue : notnull;

internal static class DeviceValueSetter
{
    public static AsyncDeviceValueSetter<TValue> AsAsync<TValue>(this DeviceValueSetter<TValue> setter)
        where TValue : notnull
    {
        return (deviceId, value, _) =>
        {
            setter(deviceId, value);
            return Task.CompletedTask;
        };
    }
}
