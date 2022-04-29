using System;
using System.Threading.Tasks;
using Neeo.Sdk.Utilities;

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

internal sealed class ValueFeature : IValueFeature
{
    private readonly Func<string, Task<object>> _getter;
    private readonly Func<string, string, Task>? _setter;

    private ValueFeature(Func<string, Task<object>> getter, Func<string, string, Task>? setter = default)
    {
        (this._getter, this._setter) = (getter, setter);
    }

    public static ValueFeature Create<TValue>(DeviceValueGetter<TValue> getter)
        where TValue : notnull => new(
        ValueFeature.WrapGetter(getter, ObjectConverter<TValue>.Default)
    );

    public static ValueFeature Create(DeviceValueGetter<bool> getter) => new(
        ValueFeature.WrapGetter(getter, BooleanBoxes.GetBox)
    );

    public static ValueFeature Create(DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter) => new(
        ValueFeature.WrapGetter(getter, BooleanBoxes.GetBox),
        ValueFeature.WrapSetter(setter, bool.Parse)
    );

    public static ValueFeature Create(DeviceValueGetter<double> getter, DeviceValueSetter<double> setter) => new(
        ValueFeature.WrapGetter(getter, ObjectConverter<double>.Default),
        ValueFeature.WrapSetter(setter, double.Parse)
    );

    public async Task<ValueResponse> GetValueAsync(string deviceId) => new(await this._getter(deviceId).ConfigureAwait(false));

    public async Task<SuccessResponse> SetValueAsync(string deviceId, string value)
    {
        await (this._setter ?? throw new NotSupportedException())(deviceId, value).ConfigureAwait(false);
        return true;
    }

    private static Func<string, Task<object>> WrapGetter<TValue>(DeviceValueGetter<TValue> getter, Converter<TValue, object> converter)
        where TValue : notnull => getter == null
        ? throw new ArgumentNullException(nameof(getter))
        : async deviceId => converter(await getter(deviceId).ConfigureAwait(false));

    private static Func<string, string, Task> WrapSetter<TValue>(DeviceValueSetter<TValue> setter, Converter<string, TValue> converter)
        where TValue : notnull => setter == null
        ? throw new ArgumentNullException(nameof(setter))
        : (deviceId, value) => setter(deviceId, converter(value));

    private static class ObjectConverter<T>
        where T : notnull
    {
        public static readonly Converter<T, object> Default = static value => value;
    }
}