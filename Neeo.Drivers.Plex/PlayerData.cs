namespace Neeo.Drivers.Plex;

public readonly record struct PlayerData(
    string Name,
    string MachineIdentifier,
    PlayerCapabilities Capabilities,
    string IPAddress,
    int Port,
    string Product,
    bool IsOnline = true
);
