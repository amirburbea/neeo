namespace Remote.Neeo.Devices
{
    public static class Device 
    {
        public static IDeviceBuilder Create(string name, DeviceType type) => new DeviceBuilder(name) { Type = type };
    }
}
