using System;

namespace Remote.Neeo.Devices
{
    public interface IDeviceAdapter
    {
        string AdapterName { get; }

        IDeviceInitializer? Initializer { get; }
    }

    internal sealed class DeviceAdapter : IDeviceAdapter
    {
        public DeviceAdapter(string adapterName, IDeviceInitializer? initializer) => (this.AdapterName, this.Initializer) = (adapterName, initializer);

        public string AdapterName { get; }

        public IDeviceInitializer? Initializer { get; }
    }
}
