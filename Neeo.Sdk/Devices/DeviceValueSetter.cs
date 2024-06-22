using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked by the NEEO Brain to asynchronously set a value on a device.
/// </summary>
/// <typeparam name="TValue">The type of the value to set on the device.</typeparam>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="value">The value to set.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task DeviceValueSetter<TValue>(string deviceId, TValue value, CancellationToken cancellationToken = default)
    where TValue : notnull;
