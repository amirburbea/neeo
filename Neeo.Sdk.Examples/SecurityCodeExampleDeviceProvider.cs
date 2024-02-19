using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Examples;

public class SecurityCodeExampleDeviceProvider : IDeviceProvider
{
    private bool _isRegistered;

    public SecurityCodeExampleDeviceProvider()
    {
        this.DeviceBuilder = Device.Create(Constants.DeviceName, DeviceType.Accessory)
            .SetSpecificName(Constants.DeviceName)
            .AddTextLabel(Constants.TextLabelName, "Logged In", deviceId => Task.FromResult(this.GetLabelText(deviceId)))
            .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, delegate { return Task.FromResult(DiscoverDevices()); })
            .EnableRegistration(
                Constants.RegistrationHeaderText,
                Constants.RegistrationDescription,
                () => Task.FromResult(this._isRegistered),
                securityCode => Task.FromResult(this.Register(securityCode))
            );
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private static DiscoveredDevice[] DiscoverDevices() => [new("code-device", "Security Code Device", true)];

    private string GetLabelText(string deviceId) => $"{deviceId} {(this._isRegistered ? "successfully" : "not")} registered";

    private RegistrationResult Register(string securityCode)
    {
        this._isRegistered = securityCode == Constants.TheAnswer;
        return this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect security code.");
    }

    private static class Constants
    {
        public const string DeviceName = "Security Code Example";
        public const string DiscoveryDescription = "This example device shows the use of security code registration and discovery.";
        public const string DiscoveryHeaderText = "NEEO SDK Example Registration via Security Code";
        public const string RegistrationDescription = "What is the answer to the Ultimate Question of Life, The Universe, and Everything? (Enter 42 to continue)";
        public const string RegistrationHeaderText = "Enter Security Code";
        public const string TextLabelName = "text-label";
        public const string TheAnswer = "42";
    }
}