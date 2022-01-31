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

/// <summary>
/// A callback which is invoked in response to favorites being requested on the NEEO remote
/// in order to allow the driver to respond accordingly.
/// </summary>
/// <param name="deviceId">The id associated with the device.</param>
/// <param name="favorite">The favorite requested.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
/// <remarks>
/// Example: Given a favorite of &quot;42&quot;, rather than invoking a button handler twice (&quot;DIGIT 4&quot;
/// followed by &quot;DIGIT 2&quot;), the handler is invoked with a single value of &quot;42&quot;.
/// </remarks>
public delegate Task FavoritesHandler(string deviceId, string favorite);