using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Examples.Devices;

public class CredentialedExampleDevice : IDeviceProvider
{
    private bool _isRegistered;

    public IDeviceBuilder ProvideDevice()
    {
        return Device.Create(Constants.DeviceName, DeviceType.Accessory)
            .SetSpecificName(Constants.DeviceName)
            .EnableDiscovery(Constants.DiscoveryHeaderText, Constants.DiscoveryDescription, this.DiscoverAsync)
            .EnableRegistration(Constants.RegistrationHeaderText, Constants.RegistrationDescription, this.QueryIsRegisteredAsync, this.RegisterAsync)
            .AddTextLabel(Constants.TextLabelName, "Logged In", this.GetLabelTextAsync);
    }

    private Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId)
    {
        return Task.FromResult(new[] { new DiscoveredDevice("credentialed-device", "Credentialed Device", true) });
    }

    private Task<string> GetLabelTextAsync(string deviceId) => Task.FromResult(this._isRegistered ? $"{deviceId} registered as {Constants.UserName}" : $"{deviceId} not registered");

    private Task<bool> QueryIsRegisteredAsync() => Task.FromResult(this._isRegistered);

    private Task<RegistrationResult> RegisterAsync(Credentials credentials)
    {
        this._isRegistered = credentials.UserName == Constants.UserName && credentials.Password == Constants.Password;
        return Task.FromResult(this._isRegistered ? RegistrationResult.Success : RegistrationResult.Failed("You entered an incorrect username and password."));
    }

    private static class Constants
    {
        public const string DeviceName = "Credentialed Registration Example";
        public const string DiscoveryDescription = "This example device shows the use of registration and discovery with a username and password.";
        public const string DiscoveryHeaderText = "NEEO SDK Example Registration via Credentials";
        public const string RegistrationDescription = "Enter user name of \"neeo\" and password of \"password\" to continue.";
        public const string RegistrationHeaderText = "Enter Credentials Below";
        public const string TextLabelName = "text-label";
        public const string UserName = "neeo";
        public const string Password = "password";
    }
}