using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Drivers.Hisense;

public class HisenseDeviceProvider : IDeviceProvider
{


    public IDeviceBuilder ProvideDevice()
    {
        const string name = "Hisense Smart TV";
        return Device.Create(name, DeviceType.TV)
            .SetSpecificName(name)
            .EnableDiscovery("Discovering TV...", "Ensure your TV is on and IP control is enabled.", this.PerformDiscoveryAsync)
            .EnableRegistration("Registering", "Enter the code", this.QueryIsRegisteredAsync, this.ProcessCredentialsAsync)
            .AddButtonHandler(this.HandleButtonAsync)
            .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.MenuAndBack | ButtonGroups.ChannelZapper | ButtonGroups.Volume | ButtonGroups.NumberPad)
            .AddButton(KnownButtons.PowerToggle);
    }

    private Task<bool> QueryIsRegisteredAsync()
    {
        throw new NotImplementedException();
    }

    private Task<RegistrationResult> ProcessCredentialsAsync(Credentials credentials)
    {
        throw new NotImplementedException();
    }

    private Task HandleButtonAsync(string deviceId, string button)
    {
        throw new NotImplementedException();
    }

    private Task<DiscoveredDevice[]> PerformDiscoveryAsync(string? optionalDeviceId)
    {
        throw new NotImplementedException();
    }
}
