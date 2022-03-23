using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Examples.Devices;

public class SecurityCodeExampleDeviceProvider : IDeviceProvider
{
    private bool _isRegistered;

    public SecurityCodeExampleDeviceProvider()
    {
        this.DeviceBuilder = Device.Create(Constants.DeviceName, DeviceType.Accessory)
            .SetSpecificName(Constants.DeviceName)
            .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, this.DiscoverAsync)
            .EnableRegistration(Constants.RegistrationHeaderText, Constants.RegistrationDescription, this.QueryIsRegisteredAsync, this.RegisterAsync)
            .AddTextLabel(Constants.TextLabelName, "Logged In", this.GetLabelTextAsync);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new[] { new DiscoveredDevice("code-device", "Security Code Device", true) });
    }

    private Task<string> GetLabelTextAsync(string deviceId) => Task.FromResult($"{deviceId} {(this._isRegistered ? "successfully" : "not")} registered");

    private Task<bool> QueryIsRegisteredAsync() => Task.FromResult(this._isRegistered);

    private Task<RegistrationResult> RegisterAsync(string securityCode)
    {
        this._isRegistered = securityCode == Constants.TheAnswer;
        return Task.FromResult(this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect security code."));
    }

    private static class Constants
    {
        public const string DeviceName = "Security Code Example";
        public const string DiscoveryDescription = "This example device shows the use of security code registration and discovery.";
        public const string DiscoveryHeaderText = "NEEO SDK Example Registration via Security Code";
        public const string RegistrationDescription = "What is the answer to the Ultimate Question of Life, The Universe, and Everything?";
        public const string RegistrationHeaderText = "Enter Security Code";
        public const string TextLabelName = "text-label";
        public const string TheAnswer = "42";
    }
}