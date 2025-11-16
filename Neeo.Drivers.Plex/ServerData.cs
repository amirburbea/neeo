namespace Neeo.Drivers.Plex;

public readonly record struct ServerData(
    string Name,
    string MachineIdentifier,
    string IPAddress,
    int Port
);
