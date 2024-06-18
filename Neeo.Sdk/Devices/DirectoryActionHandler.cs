using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Callback to perform an action when a directory item was clicked.
/// </summary>
/// <param name="deviceId">The identifier of the device.</param>
/// <param name="actionIdentifier">The identifier of the item that was clicked.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to represent the asynchronous action.</returns>
public delegate Task DirectoryActionHandler(string deviceId, string actionIdentifier, CancellationToken cancellationToken = default);
