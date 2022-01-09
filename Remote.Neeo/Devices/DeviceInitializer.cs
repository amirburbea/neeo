using System.Threading.Tasks;

namespace Remote.Neeo.Devices;

/// <summary>
/// A callback to be invoked to initialize the device adapter before making it available to the NEEO Brain.
/// </summary>
/// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task DeviceInitializer();
