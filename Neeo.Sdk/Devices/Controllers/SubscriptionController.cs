using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface ISubscriptionController : IController
{
    ControllerType IController.Type => ControllerType.Subscription;

    Task InitializeDeviceList(string[] deviceIds);

    Task SubscribeAsync(string deviceId);

    Task UnsubscribeAsync(string deviceId);
}

internal sealed record class SubscriptionController : ISubscriptionController
{
    private readonly DeviceSubscriptionAction _onDeviceAdded;
    private readonly DeviceSubscriptionAction _onDeviceRemoved;
    private readonly DeviceListInitializer _deviceListInitializer;

    public SubscriptionController(
        DeviceSubscriptionAction onDeviceAdded,
        DeviceSubscriptionAction onDeviceRemoved,
        DeviceListInitializer deviceListInitializer
    ) => (this._onDeviceAdded, this._onDeviceRemoved, this._deviceListInitializer) = (onDeviceAdded, onDeviceRemoved, deviceListInitializer);

    public Task InitializeDeviceList(string[] deviceIds) => this._deviceListInitializer(deviceIds);

    public Task SubscribeAsync(string deviceId) => this._onDeviceAdded(deviceId);

    public Task UnsubscribeAsync(string deviceId) => this._onDeviceRemoved(deviceId);
}