using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A callback to be invoked to initialize the device adapter before making it available to the NEEO Brain.
/// </summary>
/// <param name="cancellationToken">
/// A token that is canceled when the application is stopping. It is not tied to any single caller, because the initializer runs once and its result is shared.
/// </param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DeviceInitializer(CancellationToken cancellationToken);
