using System;
using System.Threading;
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
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<ValueResponse> GetValueAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the <paramref name="value"/> of the associated component for a device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<SuccessResponse> SetValueAsync(string deviceId, string value, CancellationToken cancellationToken = default);
}

internal sealed class ValueFeature(
    Func<string, CancellationToken, Task<object>> getter,
    Func<string, string, CancellationToken, Task>? setter = default
) : IValueFeature
{
    public static ValueFeature Create<TValue>(DeviceValueGetter<TValue> getter) where TValue : notnull => new(
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

    public async Task<ValueResponse> GetValueAsync(string deviceId, CancellationToken cancellationToken) => new(await getter(deviceId, cancellationToken).ConfigureAwait(false));

    public async Task<SuccessResponse> SetValueAsync(string deviceId, string value, CancellationToken cancellationToken)
    {
        await (setter ?? throw new NotSupportedException())(deviceId, value, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static Func<string, CancellationToken, Task<object>> WrapGetter<TValue>(DeviceValueGetter<TValue> getter, Converter<TValue, object> converter)
        where TValue : notnull => getter == null
        ? throw new ArgumentNullException(nameof(getter))
        : async (deviceId, cancellationToken) => converter(await getter(deviceId, cancellationToken).ConfigureAwait(false));

    private static Func<string, string, CancellationToken, Task> WrapSetter<TValue>(DeviceValueSetter<TValue> setter, Converter<string, TValue> converter)
        where TValue : notnull => setter == null
        ? throw new ArgumentNullException(nameof(setter))
        : (deviceId, value, cancellationToken) => setter(deviceId, converter(value), cancellationToken);

    private static class ObjectConverter<T>
        where T : notnull
    {
        public static readonly Converter<T, object> Default = static value => value;
    }
}
