using System;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device subscription.
/// Contains a set of callbacks to be invoked when devices are added or removed from a NEEO Brain.
/// </summary>
public interface ISubscriptionFeature : IFeature
{
    /// <summary>
    /// Asynchronously initialize the device list.
    /// </summary>
    DeviceSubscriptionListHandler DeviceListInitializer { get; }

    /// <summary>
    /// Asynchronously notifies that a device has been added.
    /// </summary>
    DeviceSubscriptionHandler OnDeviceAdded { get; }

    /// <summary>
    /// Asynchronously notifies that a device has been removed.
    /// </summary>
    DeviceSubscriptionHandler OnDeviceRemoved { get; }

    FeatureType IFeature.Type => FeatureType.Subscription;
}

internal sealed class SubscriptionFeature : ISubscriptionFeature
{
    public SubscriptionFeature(DeviceSubscriptionHandler onDeviceAdded, DeviceSubscriptionHandler onDeviceRemoved, DeviceSubscriptionListHandler initializeDeviceList)
    {
        this.OnDeviceAdded = onDeviceAdded ?? throw new ArgumentNullException(nameof(onDeviceAdded));
        this.OnDeviceRemoved = onDeviceRemoved ?? throw new ArgumentNullException(nameof(onDeviceRemoved));
        this.DeviceListInitializer = initializeDeviceList ?? throw new ArgumentNullException(nameof(initializeDeviceList));
    }

    public DeviceSubscriptionListHandler DeviceListInitializer { get; }

    public DeviceSubscriptionHandler OnDeviceAdded { get; }

    public DeviceSubscriptionHandler OnDeviceRemoved { get; }
}