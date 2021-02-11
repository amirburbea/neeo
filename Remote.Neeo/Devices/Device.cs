namespace Remote.Neeo.Devices
{
    public static class Device
    {
        /// <summary>
        /// Gets a fluent <see cref="IDeviceBuilder"/> for defining a NEEO device driver.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="type">The type of device.</param>
        /// <param name="prefix">An optional prefix to attach to the internal name (defaults to the computer host name).</param>
        /// <returns><see cref="IDeviceBuilder"/> for defining the NEEO device driver.</returns>
        public static IDeviceBuilder Create(string name, DeviceType type, string? prefix = default) => new DeviceBuilder(name, type, prefix);

        public static bool RequiresInput(this DeviceType type) => type is DeviceType.AVReceiver
            or DeviceType.HdmiSwitch
            or DeviceType.Projector
            or DeviceType.SoundBar
            or DeviceType.TV;

        public static bool SupportsFavorites(this DeviceType type) => type is DeviceType.SetTopBox
            or DeviceType.Tuner
            or DeviceType.TV;

        public static bool SupportsPlayer(this DeviceType type) => type is DeviceType.MediaPlayer
            or DeviceType.MusicPlayer
            or DeviceType.VideoOnDemand;

        public static bool SupportsTiming(this DeviceType type) => type is not DeviceType.Accessory
            or DeviceType.Light;
    }
}
