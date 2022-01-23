using System.Threading.Tasks;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Devices.Controllers;

public interface IDiscoveryController : IController
{
    ControllerType IController.Type => ControllerType.Discovery;

    Task<DiscoveryResult[]> DiscoverAsync();
}

internal sealed class DiscoveryController : IDiscoveryController
{
    private readonly string _deviceAdapterName;
    private readonly DiscoveryProcessor _processor;

    public DiscoveryController(string deviceAdapterName, DiscoveryProcessor processor)
    {
        (this._deviceAdapterName, this._processor) = (deviceAdapterName, processor);
    }

    public Task<DiscoveryResult[]> DiscoverAsync() => this._processor(this._deviceAdapterName);
}