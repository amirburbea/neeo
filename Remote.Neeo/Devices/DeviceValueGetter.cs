using System;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Callback invoked by the NEEO Brain to asynchronously get a value from a device.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="deviceId"></param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public delegate Task<TValue> DeviceValueGetter<TValue>(string deviceId)
        where TValue : notnull, IConvertible;
}
