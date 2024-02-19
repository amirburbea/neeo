using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Convenience method for creating a <see cref="DeviceValueGetter&lt;TValue&gt;"/> from a fixed value.
/// </summary>
public static class DeviceValueGetter
{
    /// <summary>
    /// Create a <see cref="DeviceValueGetter&lt;TValue&gt;"/> from a fixed value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to return from the created callback.</param>
    /// <returns><see cref="DeviceValueGetter&lt;TValue&gt;"/> for the specified <paramref name="value"/>.</returns>
    public static DeviceValueGetter<TValue> FromValue<TValue>(TValue value)
        where TValue : notnull => _ => Task.FromResult(value);
}

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously get a value from a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to get from the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<TValue> DeviceValueGetter<TValue>(string deviceId)
    where TValue : notnull;
