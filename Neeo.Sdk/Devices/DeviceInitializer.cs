using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A callback to be invoked to initialize the device adapter before making it available to the NEEO Brain.
/// </summary>
/// <param name="cancellationToken">A token to monitor for cancellation requests (defaults to <see cref="CancellationToken.None"/>).</param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DeviceInitializer(CancellationToken cancellationToken = default);