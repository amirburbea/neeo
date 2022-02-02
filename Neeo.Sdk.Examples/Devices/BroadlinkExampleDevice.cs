using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Broadlink.RM;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

internal class BroadlinkExampleDevice : IExampleDevice
{
    RMDevice? _rmDevice;

    public IDeviceBuilder Builder { get; }

    private Task OnButtonPressed(string deviceId, string button)
    {
        return Task.CompletedTask;
    }

    public BroadlinkExampleDevice()
    {
        this.Builder = Device.Create("My TV", DeviceType.TV)
            .SetSpecificName("Broadlink")
            .AddButton(KnownButtons.InputHdmi1 | KnownButtons.InputHdmi2 | KnownButtons.InputHdmi3)
            .AddButton(KnownButtons.PowerToggle)
            .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.MenuAndBack | ButtonGroups.Volume)
            .AddButtonHandler(this.OnButtonPressed)
            .RegisterInitializer(this.Initialize);
    }

    private async Task Initialize()
    {
        this._rmDevice = await RMDiscovery.DiscoverDeviceAsync().ConfigureAwait(true);
    }
}
