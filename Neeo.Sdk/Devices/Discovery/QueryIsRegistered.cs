using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Discovery;

/// <summary>
/// A callback invoked by the NEEO Brain to check whether registration has been previously
/// performed successfully.
/// <para />
/// If the task results to <see langword="true"/> then the NEEO Brain will skip registration.
/// </summary>
/// <returns>A boolean </returns>
public delegate Task<bool> QueryIsRegistered();
