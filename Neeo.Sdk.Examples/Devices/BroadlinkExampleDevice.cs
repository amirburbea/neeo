using System.Threading.Tasks;
using Broadlink.RM;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

internal class BroadlinkExampleDevice : IDeviceProvider
{
    private RMDevice? _rmDevice;

    public IDeviceBuilder ProvideDevice() => Device.Create("My TV", DeviceType.TV)
        .SetSpecificName("Broadlink")
        .AddButton(KnownButtons.InputHdmi1 | KnownButtons.InputHdmi2 | KnownButtons.InputHdmi3)
        .AddButton(KnownButtons.PowerToggle)
        .AddButtonGroup(ButtonGroups.ControlPad | ButtonGroups.MenuAndBack | ButtonGroups.Volume)
        .AddButtonHandler(this.OnButtonPressed)
        .RegisterInitializer(this.Initialize);

    private async Task Initialize()
    {
        this._rmDevice = await RMDiscovery.DiscoverDeviceAsync().ConfigureAwait(true);
    }

    private Task OnButtonPressed(string deviceId, string button)
    {
        return Task.CompletedTask;
    }
}