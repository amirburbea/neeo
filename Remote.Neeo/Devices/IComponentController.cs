using System;
using System.Threading.Tasks;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
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

        /// <summary>
        /// Asynchronously execute on the specified device.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task<object> ExecuteAsync(string deviceId, object? parameter);
    }

    internal sealed class ComponentController : IComponentController
    {
        private readonly Func<string, object?, Task<object>>? _execute;
        private readonly Func<string, Task<object>>? _getter;
        private readonly Func<string, object, Task>? _setter;

        private ComponentController(
            Func<string, Task<object>>? getter = null,
            Func<string, object, Task>? setter = null,
            Func<string, object?, Task<object>>? execute = null
        ) => (this._getter, this._setter, this._execute) = (getter, setter, execute);

        public bool CanExecute => this._execute != null;

        public bool CanGetValue => this._getter != null;

        public bool CanSetValue => this._setter != null;

        public static ComponentController Create<TValue>(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
            where TValue : notnull, IConvertible => new(
            getter == null
                ? throw new ArgumentNullException(nameof(getter))
                : async deviceId => await getter(deviceId).ConfigureAwait(false),
            setter == null
                ? default
                : (deviceId, value) => setter(deviceId, (TValue)Convert.ChangeType(value, typeof(TValue)))
        );

        public static ComponentController Create(FavoritesHandler handler) => handler == null
            ? throw new ArgumentNullException(nameof(handler))
            : new(execute: async (deviceId, value) =>
              {
                  await handler(deviceId, Convert.ToString(value)!).ConfigureAwait(false);
                  return true;
              });

        public static ComponentController Create(DiscoveryProcessor processor) => processor == null
            ? throw new ArgumentNullException(nameof(processor))
            : new(execute: async (deviceId, _) => await processor(deviceId).ConfigureAwait(false));

        public Task<object> ExecuteAsync(string deviceId, object? value) => this._execute == null
            ? throw new NotSupportedException($"{nameof(this.ExecuteAsync)} is not supported for this {nameof(ComponentController)}.")
            : this._execute(deviceId, value);

        public Task<object> GetValueAsync(string deviceId) => this._getter == null
            ? throw new NotSupportedException($"{nameof(this.GetValueAsync)} is not supported for this {nameof(ComponentController)}.")
            : this._getter(deviceId);

        public Task SetValueAsync(string deviceId, object value) => this._setter == null
            ? throw new NotSupportedException($"{nameof(this.SetValueAsync)} is not supported for this {nameof(ComponentController)}.")
            : this._setter(deviceId, value);
    }
}