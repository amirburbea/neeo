namespace Remote.Neeo.Devices.Discovery
{
    public readonly struct DiscoveryResult
    {
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
}
