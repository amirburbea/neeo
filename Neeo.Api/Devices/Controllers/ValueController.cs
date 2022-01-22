using System;
using System.Threading.Tasks;

namespace Neeo.Api.Devices.Controllers;

public interface IValueController : IController
{
    ControllerType IController.Type => ControllerType.Value;

    Task<ValueResult> GetValueAsync(string deviceId);

    Task<SuccessResult> SetValueAsync(string deviceId, object value);
}

internal abstract class ValueController : IValueController
{
    public static ValueController<TValue> Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
        where TValue : notnull, IConvertible => new(getter, setter);

    protected ValueController()
    {
    }

    public abstract Task<ValueResult> GetValueAsync(string deviceId);

    public abstract Task<SuccessResult> SetValueAsync(string deviceId, object value);
}

internal sealed class ValueController<TValue> : ValueController
    where TValue : notnull, IConvertible
{
    private readonly DeviceValueGetter<TValue> _getter;
    private readonly DeviceValueSetter<TValue>? _setter;

    public ValueController(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
    {
        (this._getter, this._setter) = (getter ?? throw new ArgumentNullException(nameof(getter)), setter);
    }

    public override async Task<ValueResult> GetValueAsync(string deviceId) => new(await this._getter(deviceId).ConfigureAwait(false));

    public override async Task<SuccessResult> SetValueAsync(string deviceId, object value)
    {
        await (this._setter ?? throw new NotSupportedException())(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)));
        return true;
    }
}