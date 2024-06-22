using System.Threading;
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
    /// Asynchronously notifies a driver of a device being added to the NEEO Brain..
    /// </summary>
    /// <param name="deviceId">The identifier of the device added.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyDeviceAddedAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously notifies a driver of the device list.
    /// </summary>
    /// <param name="deviceIds">Array of device identifiers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously notifies a driver of a device being removed from the NEEO Brain..
    /// </summary>
    /// <param name="deviceId">The identifier of the device removed.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken = default);
}

internal sealed class SubscriptionFeature(
    DeviceSubscriptionHandler notifyDeviceAdded,
    DeviceSubscriptionHandler notifyDeviceRemoved,
    DeviceSubscriptionListHandler notifyDeviceList
) : ISubscriptionFeature
{
    public Task NotifyDeviceAddedAsync(string deviceId, CancellationToken cancellationToken) => notifyDeviceAdded(deviceId, cancellationToken);

    public Task NotifyDeviceListAsync(string[] deviceIds, CancellationToken cancellationToken) => notifyDeviceList(deviceIds, cancellationToken);

    public Task NotifyDeviceRemovedAsync(string deviceId, CancellationToken cancellationToken) => notifyDeviceRemoved(deviceId, cancellationToken);
}
