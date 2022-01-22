using System.Threading.Tasks;

namespace Neeo.Api.Devices.Controllers;

public interface ISubscriptionController : IController
{
    ControllerType IController.Type => ControllerType.Subscription;

    Task InitializeDeviceList(string[] deviceIds);

    Task SubscribeAsync(string deviceId);

    Task UnsubscribeAsync(string deviceId);
}

internal sealed record class SubscriptionController : ISubscriptionController
{
    private readonly DeviceAction _onDeviceAdded;
    private readonly DeviceAction _onDeviceRemoved;
    private readonly DeviceListInitializer _deviceListInitializer;

    public SubscriptionController(DeviceAction onDeviceAdded, DeviceAction onDeviceRemoved, DeviceListInitializer deviceListInitializer)
    {
        this._onDeviceAdded = onDeviceAdded;
        this._onDeviceRemoved = onDeviceRemoved;
        this._deviceListInitializer = deviceListInitializer;
    }

    public Task InitializeDeviceList(string[] deviceIds) => this._deviceListInitializer(deviceIds);

    public Task SubscribeAsync(string deviceId) => this._onDeviceAdded(deviceId);

    public Task UnsubscribeAsync(string deviceId) => this._onDeviceRemoved(deviceId);
}