using System.Net;

namespace Neeo.Drivers.Plex;

public readonly record struct PlexServerData(string Name, string MachineIdentifier, IPAddress IPAddress);
