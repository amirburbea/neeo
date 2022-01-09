namespace Remote.Neeo.Devices.Discovery;

public record struct DiscoveryOptions(string HeaderText, string Description, bool EnableDynamicDeviceBuilder = false);
