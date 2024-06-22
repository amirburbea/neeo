using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Callback invoked by the NEEO Brain to populate a directory in order to support browsing.
/// </summary>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="builder">The directory builder.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DirectoryBrowser(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken = default);
