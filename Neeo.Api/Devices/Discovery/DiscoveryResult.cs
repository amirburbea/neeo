using System.Text.Json.Serialization;

namespace Neeo.Api.Devices.Discovery;

public readonly struct DiscoveryResult
{
    [JsonConstructor]
    public DiscoveryResult(string id, string name, bool? reachable = default)
    {
        this.Id = id;
        this.Name = name;
        this.Reachable = reachable;
    }

    public string Id { get; }

    public string Name { get; }

    public bool? Reachable { get; }
}
