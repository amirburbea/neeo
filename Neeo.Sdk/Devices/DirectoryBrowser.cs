using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback invoked by the NEEO Brain to populate a list in order to support browsing the directory.
/// </summary>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="list">The list builder.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DirectoryBrowser(string deviceId, DirectoryBuilder list, CancellationToken cancellationToken = default);
