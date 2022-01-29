using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IValueFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Value;

    Task<ValueResponse> GetValueAsync(string deviceId);

    Task<SuccessResponse> SetValueAsync(string deviceId, string value);
}

internal sealed class ValueController : IValueFeature
{
    private readonly DeviceValueGetter<ValueResponse> _getter;
    private readonly DeviceValueSetter<string>? _setter;

    private ValueController(DeviceValueGetter<ValueResponse> getter, DeviceValueSetter<string>? setter = default)
    {
        (this._getter, this._setter) = (getter, setter);
    }

    public static ValueController Create<TValue>(DeviceValueGetter<TValue> getter)
        where TValue : notnull => getter == null
        ? throw new ArgumentNullException(nameof(getter)) 
        : new(async deviceId => new(await getter(deviceId).ConfigureAwait(false)));

    public static ValueController Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue> setter)
        where TValue : notnull, IConvertible => (getter, setter) switch
        {
            (null, _) => throw new ArgumentNullException(nameof(getter)),
            (_, null) => throw new ArgumentNullException(nameof(setter)),
            _ => new(
                async deviceId => new(await getter(deviceId).ConfigureAwait(false)),
                (deviceId, value) => setter(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)))
            )
        };

    public Task<ValueResponse> GetValueAsync(string deviceId) => this._getter(deviceId);

    public async Task<SuccessResponse> SetValueAsync(string deviceId, string value)
    {
        await (this._setter ?? throw new NotSupportedException())(deviceId, value).ConfigureAwait(false);
        return new();
    }
}