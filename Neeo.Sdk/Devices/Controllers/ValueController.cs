using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IValueController : IController
{
    ControllerType IController.Type => ControllerType.Value;

    Task<object> GetValueAsync(string deviceId);

    Task SetValueAsync(string deviceId, string value);
}

internal sealed class ValueController<TValue> : IValueController
    where TValue : notnull, IConvertible
{
    private readonly DeviceValueGetter<TValue> _getter;
    private readonly DeviceValueSetter<TValue>? _setter;

    public ValueController(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
    {
        (this._getter, this._setter) = (getter ?? throw new ArgumentNullException(nameof(getter)), setter);
    }

    public async Task<object> GetValueAsync(string deviceId) => await this._getter(deviceId).ConfigureAwait(false);

    public Task SetValueAsync(string deviceId, string value) => (this._setter ?? throw new NotSupportedException())(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)));
}