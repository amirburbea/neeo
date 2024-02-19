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
    /// Asynchronously initialize the list of device identifiers.
    /// </summary>
    /// <param name="deviceIds">Array of device identifiers.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task InitializeDeviceListAsync(string[] deviceIds);

    /// <summary>
    /// Asynchronously handle a notification that a device has been added.
    /// </summary>
    /// <param name="deviceId">The device identifier of the added device.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnDeviceAddedAsync(string deviceId);

    /// <summary>
    /// Asynchronously handle a notification that a device has been removed.
    /// </summary>
    /// <param name="deviceId">The device identifier of the removed device.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnDeviceRemovedAsync(string deviceId);
}

internal sealed class SubscriptionFeature(
    DeviceSubscriptionHandler onDeviceAdded, 
    DeviceSubscriptionHandler onDeviceRemoved, 
    DeviceSubscriptionListHandler deviceListInitializer
) : ISubscriptionFeature
{
    private readonly DeviceSubscriptionListHandler _deviceListInitializer = deviceListInitializer ?? throw new ArgumentNullException(nameof(deviceListInitializer));
    private readonly DeviceSubscriptionHandler _onDeviceAdded = onDeviceAdded ?? throw new ArgumentNullException(nameof(onDeviceAdded));
    private readonly DeviceSubscriptionHandler _onDeviceRemoved = onDeviceRemoved ?? throw new ArgumentNullException(nameof(onDeviceRemoved));

    public Task InitializeDeviceListAsync(string[] deviceIds) => this._deviceListInitializer(deviceIds);

    public Task OnDeviceAddedAsync(string deviceId) => this._onDeviceAdded(deviceId);

    public Task OnDeviceRemovedAsync(string deviceId) => this._onDeviceRemoved(deviceId);
}