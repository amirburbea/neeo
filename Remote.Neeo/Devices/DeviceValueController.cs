using System;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Interface for an individual device value controller.
    /// </summary>
    public interface IDeviceValueController
    {
        /// <summary>
        /// Gets a value indicating if this controller is read-only.
        /// </summary>
        bool IsReadOnly { get; }

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

    /// <summary>
    /// Device value controller.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public sealed class DeviceValueController<TValue> : IDeviceValueController
        where TValue : notnull, IConvertible
    {
        private readonly DeviceValueGetter<TValue> _getter;
        private readonly DeviceValueSetter<TValue>? _setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceValueController{TValue}"/> class.
        /// </summary>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        public DeviceValueController(DeviceValueGetter<TValue> getter, DeviceValueSetter<TValue>? setter = default)
        {
            this._getter = getter ?? throw new ArgumentNullException(nameof(getter));
            this._setter = setter;
        }

        /// <summary>
        /// Gets a value indicating if this controller is read-only.
        /// </summary>
        public bool IsReadOnly => this._setter == null;

        /// <summary>
        /// Asynchronously get a value from the device.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<TValue> GetValueAsync(string deviceId) => this._getter(deviceId);

        Task<object> IDeviceValueController.GetValueAsync(string deviceId) => this.GetValueAsync(deviceId).ContinueWith(
            task => (object)task.Result,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion
        );

        /// <summary>
        /// Asynchronously set a value on the device.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="value">The value to set.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SetValueAsync(string deviceId, TValue value) => this._setter == null ? throw new InvalidOperationException("Controller is read only.") : this._setter(deviceId, value);

        Task IDeviceValueController.SetValueAsync(string deviceId, object value) => this.SetValueAsync(
            deviceId,
            value is TValue data ? data : (TValue)Convert.ChangeType(value, typeof(TValue))
        );
    }
}