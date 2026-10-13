using System;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Describes a driver's connection to a single physical device, for tracking in an <see cref="IDeviceRegistry{TClient}"/>.
/// </summary>
public interface IDeviceClient
{
    /// <summary>
    /// Gets the identifier of the device (as used by the NEEO Brain). It must not change while the client is registered.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Gets a value indicating whether the client is currently connected to the device.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Raised when <see cref="IsConnected"/> may have changed. Raising it without an actual change is harmless -
    /// the registry only reports actual transitions.
    /// </summary>
    event EventHandler? ConnectionChanged;
}
