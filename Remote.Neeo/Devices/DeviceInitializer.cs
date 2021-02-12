using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// A (potentially) asynchronous method to invoke in order to initialize the NEEO device driver.
    /// <para />
    /// For example: Setting up a REST endpoint for the actual device(s) to interact with.
    /// </summary>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public delegate Task DeviceInitializer();
}
