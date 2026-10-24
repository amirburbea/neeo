using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Examples;

public class CredentialsExampleDeviceProvider : IDeviceProvider
{
    private bool _isRegistered;

    public CredentialsExampleDeviceProvider() => this.DeviceBuilder = Device.Create(Constants.DeviceName, DeviceType.Accessory)
        .SetSpecificName(Constants.DeviceName)
        .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, (_, _) => Task.FromResult(this.Discover()))
        .EnableRegistration(
            Constants.RegistrationHeaderText,
            Constants.RegistrationDescription,
            (_) => Task.FromResult(this._isRegistered),
            (userName, password, _) => Task.FromResult(this.Register(userName, password))
        )
        .AddTextLabel(Constants.TextLabelName, "Logged In", this.GetLabelText);

    public IDeviceBuilder DeviceBuilder { get; }

    private DiscoveredDevice[] Discover() => [new DiscoveredDevice("credentialed-device", "Credentialed Device", true)];

    private string GetLabelText(string deviceId) => this._isRegistered ? $"{deviceId} registered as {Constants.UserName}" : $"{deviceId} not registered";

    private RegistrationResult Register(string userName, string password)
    {
        this._isRegistered = userName == Constants.UserName && password == Constants.Password;
        return this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect username and password.");
    }

    private static class Constants
    {
        public const string DeviceName = "SDK Credentials Registration Example";
        public const string DiscoveryDescription = "This example device shows the use of registration and discovery with a username and password.";
        public const string DiscoveryHeaderText = "NEEO SDK Example Registration via Credentials";
        public const string Password = "password";
        public const string RegistrationDescription = "Enter user name of \"neeo\" and password of \"password\" to continue.";
        public const string RegistrationHeaderText = "Enter Credentials Below";
        public const string TextLabelName = "text-label";
        public const string UserName = "neeo";
    }
}
