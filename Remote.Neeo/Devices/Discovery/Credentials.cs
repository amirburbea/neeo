using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Discovery
{
    public readonly struct Credentials
    {
        [JsonConstructor]
        public Credentials(string username, string password)
        {
            (this.UserName, this.Password) = (username, password);
        }

        public string Password { get; }

        [JsonPropertyName("username")]
        public string UserName { get; }
    }
}