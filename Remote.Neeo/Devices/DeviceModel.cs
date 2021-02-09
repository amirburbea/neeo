using System;

namespace Remote.Neeo.Devices
{
    public interface IDeviceModel
    {
        string AdapterName { get; }
        int Id { get; }
        string Manufacturer { get; }

        string Name { get; }

        string Tokens { get; }
        DeviceType Type { get; }
    }

    public sealed class DeviceModel : IDeviceModel
    {
        public string AdapterName => throw new NotImplementedException();
        public int Id => throw new NotImplementedException();
        public string Manufacturer => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string Tokens => throw new NotImplementedException();
        public DeviceType Type => throw new NotImplementedException();
    }
}
