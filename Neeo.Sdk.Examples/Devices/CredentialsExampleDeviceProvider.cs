using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Examples.Devices;

public class CredentialsExampleDeviceProvider : IDeviceProvider
{
    private bool _isRegistered;

    public CredentialsExampleDeviceProvider() => this.DeviceBuilder = Device.Create(Constants.DeviceName, DeviceType.Accessory)
        .SetSpecificName(Constants.DeviceName)
        .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, this.DiscoverAsync)
        .EnableRegistration(
            Constants.RegistrationHeaderText,
            Constants.RegistrationDescription,
            (_) => this.QueryIsRegisteredAsync(),
            (userName, password, _) => this.RegisterAsync(userName, password)
        )
        .AddTextLabel(Constants.TextLabelName, "Logged In", (deviceId, _) => this.GetLabelTextAsync(deviceId));

    public IDeviceBuilder DeviceBuilder { get; }

    private Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new[] { new DiscoveredDevice("credentialed-device", "Credentialed Device", true) });
    }

    private Task<string> GetLabelTextAsync(string deviceId) => Task.FromResult(this._isRegistered ? $"{deviceId} registered as {Constants.UserName}" : $"{deviceId} not registered");

    private Task<bool> QueryIsRegisteredAsync() => Task.FromResult(this._isRegistered);

    private Task<RegistrationResult> RegisterAsync(string userName, string password)
    {
        this._isRegistered = userName == Constants.UserName && password == Constants.Password;
        return Task.FromResult(this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect username and password."));
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
