namespace Neeo.Api.Devices.Discovery;

public sealed record class DiscoveryResult(string Id, string Name, bool? Reachable = default);