namespace Remote.Neeo.Devices.Discovery
{
    public readonly struct DiscoveryOptions
    {
        public DiscoveryOptions(string headerText, string description, bool enableDynamicDeviceBuilder = false)
        {
            this.HeaderText = headerText;
            this.Description = description;
            this.EnableDynamicDeviceBuilder = enableDynamicDeviceBuilder;
        }

        public string Description { get; }

        public bool EnableDynamicDeviceBuilder { get; }

        public string HeaderText { get; }
    }
}
