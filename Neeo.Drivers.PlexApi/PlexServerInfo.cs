using System.Net;

namespace Neeo.Drivers.PlexApi;

public readonly record struct PlexServerInfo(string Name, IPAddress IPAddress);
