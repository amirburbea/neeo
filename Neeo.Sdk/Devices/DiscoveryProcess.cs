using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Defines the process by which the driver can discover additional devices, such as over the network.
/// </summary>
/// <param name="optionalDeviceId">
/// If not <see langword="null"/>, the results should be filtered to the single specified device identifier.
/// </param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<DiscoveredDevice[]> DiscoveryProcess(string? optionalDeviceId = default, CancellationToken cancellationToken = default);