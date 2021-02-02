using System;

namespace Remote.Neeo
{
    public record Brain
    {
        public Brain(string ipAddress, int port, string name, string hostName, string version, string region, DateTime updated)
        {
            (this.IPAddress, this.Port, this.Name, this.HostName, this.Version, this.Region, this.Updated) = (ipAddress, port, name, hostName, version, region, updated);
        }

        public string HostName { get; }

        public string IPAddress { get; }

        public string Name { get; }

        public int Port { get; }

        public string Region { get; }

        public DateTime Updated { get; }

        public string Version { get; }
    }
}
