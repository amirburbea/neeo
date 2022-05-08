using Microsoft.Extensions.Logging.Abstractions;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Tests.Devices;

// TODO: Implement dynamic device registry tests.
public sealed class DynamicDeviceRegistryTests
{
    private readonly DynamicDeviceRegistry _dynamicDeviceRegistry = new(NullLogger<DynamicDeviceRegistry>.Instance);
}