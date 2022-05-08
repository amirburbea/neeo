using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;

namespace Neeo.Sdk.Tests.Devices;

// TODO: Implement device database tests.
public sealed class DeviceDatabaseTests
{
    private readonly Mock<INotificationService> _mockNotificationService = new(MockBehavior.Strict);

    private DeviceDatabase CreateDatabase(IReadOnlyCollection<IDeviceBuilder> devices) => new(devices, this._mockNotificationService.Object, NullLogger<DeviceDatabase>.Instance);
}