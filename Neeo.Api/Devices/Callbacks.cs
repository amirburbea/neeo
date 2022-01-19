using System;
using System.Threading.Tasks;

namespace Neeo.Api.Devices;

/// <summary>
/// A callback which is invoked in response to buttons being pressed on the NEEO remote
/// in order to allow the driver to respond accordingly.
/// <para />
/// </summary>
/// <param name="deviceId">The id associated with the device.</param>
/// <param name="button">
/// The name of the button being pressed.
/// <para/>
/// Note that <see cref="KnownButton.TryGetKnownButton"/> may be able to translate this into a <see cref="KnownButtons"/> enumerated value.
/// </param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task ButtonHandler(string deviceId, string button);

public delegate Task DeviceAction(string deviceId);

/// <summary>
/// A callback to be invoked to initialize the device adapter before making it available to the NEEO Brain.
/// </summary>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DeviceInitializer();

/// <summary>
/// Callback function used on startup once the SDK can reach the Brain, this is called on startup with the current
/// subscriptions removing the need to save them in the driver.
/// </summary>
/// <param name="deviceIds">Array of deviceId string for all devices of this type currently on the Brain.</param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DeviceListInitializer(string[] deviceIds);

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously get a value from a device.
/// </summary>
/// <typeparam name="TValue"></typeparam>
/// <param name="deviceId"></param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<TValue> DeviceValueGetter<TValue>(string deviceId)
    where TValue : notnull, IConvertible;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously set a value on a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to set on the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="value">The value to set.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task DeviceValueSetter<TValue>(string deviceId, TValue value)
    where TValue : notnull, IConvertible;

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

public delegate void SubscriptionFunction(UpdateCallback updateCallback, IPowerCallbacks powerCallbacs);

public delegate Task UpdateCallback(Message message);