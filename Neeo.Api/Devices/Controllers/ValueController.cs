using System;
using System.Threading.Tasks;

namespace Neeo.Api.Devices.Controllers;

public interface IValueController : IController
{
    ControllerType IController.Type => ControllerType.Value;

    Task<ValueResult> GetValueAsync(string deviceId);

    Task<SuccessResult> SetValueAsync(string deviceId, object value);
}

internal sealed class ValueController<TValue>: IValueController where TValue : notnull, IConvertible
{
    private readonly DeviceValueGetter<TValue> _getter;
    private readonly DeviceValueSetter<TValue>? _setter;

    public ValueController(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default) => (this._getter, this._setter) = (getter, setter);

    public async Task<ValueResult> GetValueAsync(string deviceId) => new(await this._getter(deviceId).ConfigureAwait(false));

    public async Task<SuccessResult> SetValueAsync(string deviceId, object value)
    {
        await (this._setter ?? throw new NotSupportedException())(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)));
        return true;
    }
}