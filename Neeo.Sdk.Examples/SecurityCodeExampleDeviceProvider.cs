using System.Threading;
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
            .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, (_, _) => Task.FromResult(this.Discover()))
            .EnableRegistration(Constants.RegistrationHeaderText, Constants.RegistrationDescription, _ => Task.FromResult(this._isRegistered), (code, _) => Task.FromResult(this.Register(code)))
            .AddTextLabel(Constants.TextLabelName, "Logged In", this.GetLabelText);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private DiscoveredDevice[] Discover() => [new DiscoveredDevice("code-device", "Security Code Device", true)];

    private string GetLabelText(string deviceId) =>
        $"{deviceId} {(this._isRegistered ? "successfully" : "not")} registered";

    private RegistrationResult Register(string securityCode)
    {
        this._isRegistered = securityCode == "42";
        return this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect security code.");
    }

    private static class Constants
    {
        public const string DeviceName = "SDK Security Code Example";
        public const string DiscoveryDescription = "This example device shows the use of security code registration and discovery.";
        public const string DiscoveryHeaderText = "NEEO SDK Example Registration via Security Code";
        public const string RegistrationDescription = "What is the answer to the Ultimate Question of Life, The Universe, and Everything? (42)";
        public const string RegistrationHeaderText = "Enter Security Code";
        public const string TextLabelName = "text-label";
    }
}
