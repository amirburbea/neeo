using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device subscription.
/// Contains a set of callbacks to be invoked when devices are added or removed from a NEEO Brain.
/// </summary>
public interface ISubscriptionFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Subscription;

    /// <summary>
    /// Asynchronously initialize the device list.
    /// </summary>
    /// <param name="deviceIds">The device identifiers.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task InitializeDeviceListAsync(string[] deviceIds);

    /// <summary>
    /// Asynchronously notifies that a device has been added.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyDeviceAddedAsync(string deviceId);

    /// <summary>
    /// Asynchronously notifies that a device has been removed.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyDeviceRemovedAsync(string deviceId);
}

internal sealed record class SubscriptionFeature : ISubscriptionFeature
{
    private readonly DeviceSubscriptionHandler _onDeviceAdded;
    private readonly DeviceSubscriptionHandler _onDeviceRemoved;
    private readonly DeviceSubscriptionListHandler _deviceListInitializer;

    public SubscriptionFeature(
        DeviceSubscriptionHandler onDeviceAdded,
        DeviceSubscriptionHandler onDeviceRemoved,
        DeviceSubscriptionListHandler initializeDeviceList
    )
    {
        this._onDeviceAdded = onDeviceAdded ?? throw new ArgumentNullException(nameof(onDeviceAdded));
        this._onDeviceRemoved = onDeviceRemoved ?? throw new ArgumentNullException(nameof(onDeviceRemoved));
        this._deviceListInitializer = initializeDeviceList ?? throw new ArgumentNullException(nameof(initializeDeviceList));
    }

    public Task InitializeDeviceListAsync(string[] deviceIds) => this._deviceListInitializer(deviceIds);

    public Task NotifyDeviceAddedAsync(string deviceId) => this._onDeviceAdded(deviceId);

    public Task NotifyDeviceRemovedAsync(string deviceId) => this._onDeviceRemoved(deviceId);
}