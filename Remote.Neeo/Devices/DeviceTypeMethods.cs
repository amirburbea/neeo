namespace Remote.Neeo.Devices
{
    public static class DeviceTypeMethods
    {
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

        public static bool SupportsDelays(this DeviceType type) => type is not DeviceType.Accessory 
            or DeviceType.Light;
    }
}
