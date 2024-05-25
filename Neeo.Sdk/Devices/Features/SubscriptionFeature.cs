using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device subscription.
/// Contains a set of callbacks to be invoked when devices are added or removed from a NEEO Brain.
/// </summary>
public interface ISubscriptionFeature : IFeature
{
    /// <summary>
    /// Asynchronously notifies that a device has been added.
    /// </summary>
    DeviceSubscriptionHandler OnDeviceAdded { get; }

    /// <summary>
    /// Asynchronously notifies that a device has been removed.
    /// </summary>
    DeviceSubscriptionHandler OnDeviceRemoved { get; }

    FeatureType IFeature.Type => FeatureType.Subscription;

    /// <summary>
    /// Asynchronously initialize the device list.
    /// </summary>
    /// <param name="deviceIds">Array of device identifiers.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task InitializeDeviceListAsync(string[] deviceIds);
}

internal sealed class SubscriptionFeature(DeviceSubscriptionHandler onDeviceAdded, DeviceSubscriptionHandler onDeviceRemoved, DeviceSubscriptionListHandler deviceListInitializer) : ISubscriptionFeature
{
    private readonly DeviceSubscriptionListHandler _deviceListInitializer = deviceListInitializer ?? throw new ArgumentNullException(nameof(deviceListInitializer));

    public DeviceSubscriptionHandler OnDeviceAdded { get; } = onDeviceAdded ?? throw new ArgumentNullException(nameof(onDeviceAdded));

    public DeviceSubscriptionHandler OnDeviceRemoved { get; } = onDeviceRemoved ?? throw new ArgumentNullException(nameof(onDeviceRemoved));

    public Task InitializeDeviceListAsync(string[] deviceIds) => this._deviceListInitializer(deviceIds);
}
