using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IValueController : IController
{
    ControllerType IController.Type => ControllerType.Value;

    Task<object> GetValueAsync(string deviceId);

    Task SetValueAsync(string deviceId, string value);
}

internal sealed class ValueController : IValueController
{
    private readonly DeviceValueGetter<object> _getter;
    private readonly DeviceValueSetter<string>? _setter;

    private ValueController(DeviceValueGetter<object> getter, DeviceValueSetter<string>? setter = default)
    {
        (this._getter, this._setter) = (getter, setter);
    }

    public static ValueController Create<TValue>(DeviceValueGetter<TValue> getter)
        where TValue : notnull => getter == null
        ? throw new ArgumentNullException(nameof(getter)) 
        : new(async deviceId => await getter(deviceId).ConfigureAwait(false));

    public static ValueController Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue> setter)
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