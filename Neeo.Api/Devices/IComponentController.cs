using System;
using System.Threading.Tasks;
using Neeo.Api.Devices.Discovery;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

/// <summary>
/// Interface for an individual device component controller.
/// </summary>
public interface IComponentController
{
    /// <summary>
    /// Gets a value indicating if this controller has an execution handler.
    /// </summary>
    bool CanExecute { get; }

    /// <summary>
    /// Gets a value indicating if this controller has a value getter.
    /// </summary>
    bool CanGetValue { get; }

    /// <summary>
    /// Gets a value indicating if this controller has a value setter.
    /// </summary>
    bool CanSetValue { get; }

    /// <summary>
    /// Asynchronously execute on the specified device.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    Task<object> ExecuteAsync(string deviceId, object? parameter = default);

    /// <summary>
    /// Asynchronously get a value from the device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<object> GetValueAsync(string deviceId);

    /// <summary>
    /// Asynchronously set a value on the device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetValueAsync(string deviceId, object value);
}

public sealed class ComponentController : IComponentController
{
    private readonly Func<string, object?, Task<object>> _execute;

    private ComponentController(Func<string, object?, Task<object>> execute) => this._execute = execute;

    public bool CanExecute => true;

    bool IComponentController.CanGetValue => false;

    bool IComponentController.CanSetValue => false;

    public static ComponentController<TValue> Create<TValue>(DeviceValueGetter<TValue>? getter = default, DeviceValueSetter<TValue>? setter = default)
        where TValue : notnull, IConvertible => new(getter, setter);

    public static ComponentController Create(FavoritesHandler favoritesHandler) => favoritesHandler == null
        ? throw new ArgumentNullException(nameof(favoritesHandler))
        : new((deviceId, parameter) => favoritesHandler(deviceId, Convert.ToString(parameter)!).ContinueWith(task => BooleanBoxes.GetBox(task.Result), TaskContinuationOptions.ExecuteSynchronously));

    public static ComponentController Create(DiscoveryProcessor discoveryProcessor) => discoveryProcessor is null
        ? throw new ArgumentNullException(nameof(discoveryProcessor))
        : new(async (deviceId, _) => await discoveryProcessor(deviceId).ConfigureAwait(false));

    public static ComponentController Create(ButtonHandler buttonHandler, string button) => buttonHandler is null || button is null
        ? throw new ArgumentNullException(buttonHandler is null ? nameof(buttonHandler) : nameof(button))
        : new((deviceId, _) => buttonHandler(deviceId, button).ContinueWith(_ => BooleanBoxes.True, TaskContinuationOptions.ExecuteSynchronously));

    Task<object> IComponentController.ExecuteAsync(string deviceId, object? parameter) => this._execute(deviceId, parameter);

    Task<object> IComponentController.GetValueAsync(string deviceId) => throw new NotImplementedException();

    Task IComponentController.SetValueAsync(string deviceId, object value) => throw new NotImplementedException();
}

public sealed record class ComponentController<TValue>(
    DeviceValueGetter<TValue>? Getter = null,
    DeviceValueSetter<TValue>? Setter = null
) : IComponentController where TValue : notnull, IConvertible
{
    bool IComponentController.CanExecute => false;

    public bool CanGetValue => this.Getter != null;

    public bool CanSetValue => this.Setter != null;

    Task<object> IComponentController.ExecuteAsync(string deviceId, object? parameter) => throw new NotSupportedException();

    public async Task<object> GetValueAsync(string deviceId) => await (this.Getter ?? throw new NotSupportedException())
        .Invoke(deviceId)
        .ConfigureAwait(false);

    public Task SetValueAsync(string deviceId, object value) => (this.Setter ?? throw new NotSupportedException())
        .Invoke(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)));
}

//internal sealed class ComponentController : IComponentController
//{
//    private readonly Func<string, object?, Task<object>>? _execute;
//    private readonly Func<string, Task<object>>? _getter;
//    private readonly Func<string, object, Task>? _setter;

//    private ComponentController(
//        Func<string, Task<object>>? getter = null,
//        Func<string, object, Task>? setter = null,
//        Func<string, object?, Task<object>>? execute = null
//    ) => (this._getter, this._setter, this._execute) = (getter, setter, execute);

//    public bool CanExecute => this._execute != null;

//    public bool CanGetValue => this._getter != null;

//    public bool CanSetValue => this._setter != null;

//    public static ComponentController Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
//        where TValue : notnull, IConvertible => new(
//        getter == null
//            ? throw new ArgumentNullException(nameof(getter))
//            : async deviceId => await getter(deviceId).ConfigureAwait(false),
//        setter == null
//            ? default
//            : (deviceId, value) => setter(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)))
//    );

//    public static ComponentController Create(FavoritesHandler handler) => handler == null
//        ? throw new ArgumentNullException(nameof(handler))
//        : new(execute: async (deviceId, value) =>
//          {
//              await handler(deviceId, Convert.ToString(value)!).ConfigureAwait(false);
//              return true;
//          });

//    public static ComponentController Create(DiscoveryProcessor processor) => processor == null
//        ? throw new ArgumentNullException(nameof(processor))
//        : new(execute: async (deviceId, _) => await processor(deviceId).ConfigureAwait(false));

//    public Task<object> ExecuteAsync(string deviceId, object? value) => this._execute == null
//        ? throw new NotSupportedException($"{nameof(this.ExecuteAsync)} is not supported for this {nameof(ComponentController)}.")
//        : this._execute(deviceId, value);

//    public Task<object> GetValueAsync(string deviceId) => this._getter == null
//        ? throw new NotSupportedException($"{nameof(this.GetValueAsync)} is not supported for this {nameof(ComponentController)}.")
//        : this._getter(deviceId);

//    public Task SetValueAsync(string deviceId, object value) => this._setter == null
//        ? throw new NotSupportedException($"{nameof(this.SetValueAsync)} is not supported for this {nameof(ComponentController)}.")
//        : this._setter(deviceId, value);
//}