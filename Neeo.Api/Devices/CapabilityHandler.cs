namespace Neeo.Api.Devices;

public interface ICapabilityHandler
{
    ComponentType ComponentType { get; }

    IComponentController Controller { get; }
}

internal sealed record class CapabilityHandler(ComponentType ComponentType, IComponentController Controller) : ICapabilityHandler;