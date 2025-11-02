using System.Net;

namespace Neeo.Drivers.PlexApi;

public readonly record struct PlexPlayerInfo(
    string Name,
    string MachineIdentifier,
    PlayerCapabilities Capabilities,
    IPAddress IPAddress,
    int Port,
    string Product
);
