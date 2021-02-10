namespace Remote.Neeo.Devices
{
    public interface IDeviceModel
    {
        string AdapterName { get; }

        uint? DriverVersion { get; }

        int Id { get; }

        string Manufacturer { get; }

        string Name { get; }

        string Tokens { get; }

        DeviceType Type { get; }
    }

    internal sealed class DeviceModel : IDeviceModel
    {
        public DeviceModel(
            int id, 
            string adapterName, 
            DeviceType type, 
            string name, 
            uint? driverVersion,
            string manufacturer, 
            string tokens
        )
        {
            this.Id = id;
            this.AdapterName = adapterName;
            this.Type = type;
            this.Name = name;
            this.DriverVersion = driverVersion;
            this.Manufacturer = manufacturer;
            this.Tokens = tokens;
        }

        public string AdapterName { get; }

        public uint? DriverVersion { get; }

        public int Id { get; }

        public string Manufacturer { get; }

        public string Name { get; }

        public string Tokens { get; }

        public DeviceType Type { get; }
    }
}
