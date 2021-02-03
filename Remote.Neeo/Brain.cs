using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Remote.Neeo
{
    public record Brain
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        internal Brain(IPAddress ipAddress, int port, string name, string hostName, string version, string region, DateTime updated)
        {
            (this.IPAddress, this.Port, this.Name, this.HostName, this.Version, this.Region, this.Updated) = (ipAddress, port, name, hostName, version, region, updated);
        }

        public string HostName { get; }

        public IPAddress IPAddress { get; }

        public string Name { get; }

        public int Port { get; }

        public string Region { get; }

        public DateTime Updated { get; }

        public string Version { get; }

        internal async Task<string> GetAsync(string suffix)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.GetAsync(this.GetApiUri(suffix)).ConfigureAwait(false);
            return await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        internal async Task<string> PostAsync<T>(string suffix, T body)
        {
            using HttpClient client = new();
            HttpResponseMessage message = await client.PostAsync(
                this.GetApiUri(suffix),
                new StringContent(JsonSerializer.Serialize(body, Brain._jsonOptions), Encoding.UTF8, "application/json")
            );
            return await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private string GetApiUri(string suffix) => $"http://{this.HostName}:{this.Port}/v1/api/{suffix}";
    }
}
