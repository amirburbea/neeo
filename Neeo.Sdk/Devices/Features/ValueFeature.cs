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
    Task<ValueResponse> GetValueAsync(string deviceId);

    /// <summary>
    /// Asynchronously sets the <paramref name="value"/> of the associated component for a device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<SuccessResponse> SetValueAsync(string deviceId, string value);
}

internal abstract class ValueFeature : IValueFeature
{
    public static ValueFeature<TValue> Create<TValue>(DeviceValueGetter<TValue> getter)
        where TValue : notnull
    {
        return new(getter ?? throw new ArgumentNullException(nameof(getter)));
    }

    public static ValueFeature<TValue> Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue> setter)
         where TValue : notnull, IConvertible
    {
        return new(getter ?? throw new ArgumentNullException(nameof(getter)), setter ?? throw new ArgumentNullException(nameof(setter)));
    }

    public abstract Task<ValueResponse> GetValueAsync(string deviceId);

    public abstract Task<SuccessResponse> SetValueAsync(string deviceId, string value);
}

internal sealed class ValueFeature<TValue> : ValueFeature
    where TValue : notnull
{
    private readonly DeviceValueGetter<TValue> _getter;
    private readonly DeviceValueSetter<TValue>? _setter;

    public ValueFeature(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
    {
        (this._getter, this._setter) = (getter, setter);
    }

    public override async Task<ValueResponse> GetValueAsync(string deviceId) => new(await this._getter(deviceId).ConfigureAwait(false));

    public override async Task<SuccessResponse> SetValueAsync(string deviceId, string value)
    {
        await (this._setter ?? throw new NotSupportedException())(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue))).ConfigureAwait(false);
        return true;
    }
}
