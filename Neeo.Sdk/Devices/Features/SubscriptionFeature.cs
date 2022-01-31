using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

public interface ISubscriptionFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Subscription;

    Task InitializeDeviceList(string[] deviceIds);

    Task SubscribeAsync(string deviceId);

    Task UnsubscribeAsync(string deviceId);
}

internal sealed record class SubscriptionFeature : ISubscriptionFeature
{
    private readonly DeviceSubscriptionHandler _onDeviceAdded;
    private readonly DeviceSubscriptionHandler _onDeviceRemoved;
    private readonly DeviceSubscriptionListHandler _deviceListInitializer;

    public SubscriptionFeature(
        DeviceSubscriptionHandler onDeviceAdded,
        DeviceSubscriptionHandler onDeviceRemoved,
        DeviceSubscriptionListHandler deviceListInitializer
    ) => (this._onDeviceAdded, this._onDeviceRemoved, this._deviceListInitializer) = (onDeviceAdded, onDeviceRemoved, deviceListInitializer);

    public Task InitializeDeviceList(string[] deviceIds) => this._deviceListInitializer(deviceIds);

    public Task SubscribeAsync(string deviceId) => this._onDeviceAdded(deviceId);

    public Task UnsubscribeAsync(string deviceId) => this._onDeviceRemoved(deviceId);
}