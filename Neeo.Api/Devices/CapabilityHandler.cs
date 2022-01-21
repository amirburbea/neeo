using Neeo.Api.Devices.Controllers;

namespace Neeo.Api.Devices;

public interface ICapabilityHandler
{
    ComponentType ComponentType { get; }

    IController Controller { get; }
}

internal sealed record class CapabilityHandler(ComponentType ComponentType, IController Controller) : ICapabilityHandler;