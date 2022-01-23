using System;
using System.Threading.Tasks;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Discovery;

namespace Remote.HodgePodge;

public class DiscoveryExampleDeviceProvider : IDeviceProvider
{
    private const string THE_ANSWER = "42";

    private Credentials? _credentials;

    public IDeviceBuilder ProvideDevice()
    {
        return Device.CreateDevice("Security Code Example", DeviceType.Accessory)
            .SetManufacturer("NEEO")
            .AddAdditionalSearchTokens("SDK")
            .AddTextLabel("the-answer", "The answer is", true, GetLabelValue)
            .EnableDiscovery(
                "NEEO SDK Example Registration",
                "This example device shows the use of security code registration and discovery",
                DiscoverDevices
            )
            .RegisterDeviceSubscriptionCallbacks(OnDeviceAdded, OnDeviceRemoved, InitializeDeviceList)
            .EnableRegistration(
                "Enter Security Code",
                "What is the answer to the Ultimate Question of Life, The Universe, and Everything?",
                IsRegistered,
                Register
            );
    }

    private Task InitializeDeviceList(string[] deviceIds)
    {
        return Task.CompletedTask;
    }

    private Task OnDeviceRemoved(string deviceId)
    {
        return Task.CompletedTask;
    }

    private Task OnDeviceAdded(string deviceId)
    {
        return Task.CompletedTask;
    }

    private Task<DiscoveryResult[]> DiscoverDevices(string deviceAdapterName)
    {
        Console.WriteLine("Discovering devices");
        return Task.FromResult(new[]
        {
            new DiscoveryResult(THE_ANSWER,"Security Code Device",true)
        });
    }

    private Task<string> GetLabelValue(string deviceId)
    {
        string text = this._credentials?.UserName ?? "unregistered";
        Console.WriteLine("Getting label value {0}", text);
        return Task.FromResult(text);
    }

    private Task<bool> IsRegistered()
    {
        Console.WriteLine("Is Registered {0}", this._credentials != null);
        return Task.FromResult(this._credentials != null);
    }

    private Task Register(Credentials credentials)
    {
        if (credentials.Password != THE_ANSWER || credentials.UserName.Length==0)
        {
            throw new("This is fucked");
        }
        this._credentials=credentials;
        return Task.CompletedTask;
    }
}