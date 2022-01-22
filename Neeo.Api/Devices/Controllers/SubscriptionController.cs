using System.Threading.Tasks;

namespace Neeo.Api.Devices.Controllers;

public interface ISubscriptionController : IController
{
    ControllerType IController.Type => ControllerType.Subscription;

    Task InitializeDeviceListAsync(string[] deviceIds);

    Task<SuccessResult> SubscribeAsync(string deviceId);

    Task<SuccessResult> UnsubscribeAsync(string deviceId);
}

internal sealed record class SubscriptionController : ISubscriptionController
{
    private readonly DeviceAction _onDeviceAdded;
    private readonly DeviceAction _onDeviceRemoved;
    private readonly DeviceListInitializer _listInitializer;

    public SubscriptionController(DeviceAction onDeviceAdded, DeviceAction onDeviceRemoved, DeviceListInitializer listInitializer)
    {
        this._onDeviceAdded = onDeviceAdded;
        this._onDeviceRemoved = onDeviceRemoved;
        this._listInitializer = listInitializer;
    }

    public Task InitializeDeviceListAsync(string[] deviceIds) => this._listInitializer(deviceIds);

    public async Task<SuccessResult> SubscribeAsync(string deviceId)
    {
        await this._onDeviceAdded(deviceId).ConfigureAwait(false);
        return true;
    }

    public async Task<SuccessResult> UnsubscribeAsync(string deviceId)
    {
        await this._onDeviceRemoved(deviceId).ConfigureAwait(false);
        return true;
    }
}