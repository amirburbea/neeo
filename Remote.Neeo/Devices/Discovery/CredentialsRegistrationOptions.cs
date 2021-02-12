using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices.Discovery
{
    public sealed record Credentials
    {
        public Credentials(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        [JsonPropertyName("username")]
        public string UserName { get; }
        public string Password { get; }
    }

    public delegate Task CredentialsRegistration(Credentials credentials);

    public sealed class CredentialsRegistrationOptions : RegistrationOptions
    {
        public CredentialsRegistrationOptions()
            : base(RegistrationType.Credentials)
        {
        }
    }
}
