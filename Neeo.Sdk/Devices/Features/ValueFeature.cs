using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for getting and setting values for a device (examples include volume, power state, etc...).
/// </summary>
public interface IValueFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Value;

    /// <summary>
    /// Asynchronously gets the value of the associated component for a device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<object> GetValueAsync(string deviceId);

    /// <summary>
    /// Asynchronously sets the <paramref name="value"/> of the associated component for a device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetValueAsync(string deviceId, string value);
}

internal sealed class ValueFeature : IValueFeature
{
    private readonly DeviceValueGetter<object> _getter;
    private readonly DeviceValueSetter<string>? _setter;

    private ValueFeature(DeviceValueGetter<object> getter, DeviceValueSetter<string>? setter = default)
    {
        (this._getter, this._setter) = (getter, setter);
    }

    public static ValueFeature Create<TValue>(DeviceValueGetter<TValue> getter)
        where TValue : notnull => getter == null
        ? throw new ArgumentNullException(nameof(getter))
        : new(async deviceId => await getter(deviceId).ConfigureAwait(false));

    public static ValueFeature Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue> setter)
        where TValue : notnull, IConvertible => (getter, setter) switch
        {
            (null, _) => throw new ArgumentNullException(nameof(getter)),
            (_, null) => throw new ArgumentNullException(nameof(setter)),
            _ => new(
                async deviceId => await getter(deviceId).ConfigureAwait(false),
                (deviceId, value) => setter(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)))
            )
        };

    public Task<object> GetValueAsync(string deviceId) => this._getter(deviceId);

    public Task SetValueAsync(string deviceId, string value) => (this._setter ?? throw new NotSupportedException())(deviceId, value);
}