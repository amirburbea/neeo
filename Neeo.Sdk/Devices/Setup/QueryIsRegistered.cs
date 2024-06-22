using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Setup;

/// <summary>
/// A callback invoked by the NEEO Brain to check whether registration has been previously
/// performed successfully.
/// <para />
/// If the task result is <see langword="true"/> then the NEEO Brain will skip registration.
/// </summary>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<bool> QueryIsRegistered(CancellationToken cancellationToken = default);
