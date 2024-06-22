using System;

namespace Neeo.Drivers.Plex;

public interface IPlexSettings
{
}

partial class PlexSettings : IPlexSettings, IDisposable
{
    // Save settings when the host ends.
    void IDisposable.Dispose() => this.Save();
}
