using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Provides a stub implementation of <see cref="IDeviceNotifier"/>.
/// </summary>
public sealed class StubDeviceNotifier : IDeviceNotifier
{
    /// <summary>
    /// The sole instance of <see cref="StubDeviceNotifier"/>.
    /// </summary>
    public static readonly IDeviceNotifier Instance = new StubDeviceNotifier();

    private StubDeviceNotifier()
    {
    }

    Task IDeviceNotifier.SendNotificationAsync(
        string componentName,
        object value,
        string deviceId,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    Task IDeviceNotifier.SendPowerNotificationAsync(
        bool powerState,
        string deviceId,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;
}
