using System;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Plex;

/// <summary>
/// Adapts an <see cref="IPlexServer"/> (which is typically a runtime-generated proxy, so can not implement
/// <see cref="IDeviceClient"/> itself) for tracking in a <see cref="DeviceRegistry{TClient}"/>.
/// Disposing it disposes the server.
/// </summary>
internal sealed class PlexServerClient : IDeviceClient, IDisposable
{
    private readonly IDisposable _subscription;

    public PlexServerClient(IPlexServer server)
    {
        this.Server = server;
        this._subscription = server.IsConnectedChanged.Subscribe(_ => this.ConnectionChanged?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? ConnectionChanged;

    public string DeviceId => this.Server.MachineIdentifier;

    public bool IsConnected => this.Server.IsConnected;

    public IPlexServer Server { get; }

    public void Dispose()
    {
        this._subscription.Dispose();
        this.Server.Dispose();
    }
}
