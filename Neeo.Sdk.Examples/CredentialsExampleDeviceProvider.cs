using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Examples;

public class CredentialsExampleDeviceProvider : IDeviceProvider
{
    private bool _isRegistered;

    public CredentialsExampleDeviceProvider() => this.DeviceBuilder = Device.Create(Constants.DeviceName, DeviceType.Accessory)
        .SetSpecificName(Constants.DeviceName)
        .AddTextLabel(Constants.TextLabelName, "Logged In", deviceId => Task.FromResult(this.GetLabelText(deviceId)))
        .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, delegate { return Task.FromResult(DiscoverDevices()); })
        .EnableRegistration(
            Constants.RegistrationHeaderText,
            Constants.RegistrationDescription,
            () => Task.FromResult(this._isRegistered),
            (userName, password) => Task.FromResult(this.Register(userName, password))
        );

    public IDeviceBuilder DeviceBuilder { get; }

    private static DiscoveredDevice[] DiscoverDevices() => [new("credentialed-device", "Credentialed Device", true)];

    private string GetLabelText(string deviceId) => this._isRegistered ? $"{deviceId} registered as {Constants.UserName}" : $"{deviceId} not registered";

    private RegistrationResult Register(string userName, string password)
    {
        this._isRegistered = userName == Constants.UserName && password == Constants.Password;
        return this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect username and password.");
    }

    private static class Constants
    {
        public const string DeviceName = "Credentials Registration Example";
        public const string DiscoveryDescription = "This example device shows the use of registration and discovery with a username and password.";
        public const string DiscoveryHeaderText = "NEEO SDK Example Registration via Credentials";
        public const string Password = "password";
        public const string RegistrationDescription = "Enter user name of \"neeo\" and password of \"password\" to continue.";
        public const string RegistrationHeaderText = "Enter Credentials Below";
        public const string TextLabelName = "text-label";
        public const string UserName = "neeo";
    }
}