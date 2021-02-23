using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Remote.Neeo.Json;

namespace Remote.Neeo.Devices.Discovery
{
    public sealed class RegistrationController
    {
        public RegistrationController(QueryIsRegistered queryIsRegistered, CredentialsProcessor processor) : this(
            RegistrationType.Credentials,
            queryIsRegistered,
            processor == null ? throw new ArgumentNullException(nameof(processor)) : element => processor(element.ToObject<Credentials>())
        )
        {
        }

        public RegistrationController(QueryIsRegistered queryIsRegistered, SecurityCodeProcessor processor) : this(
            RegistrationType.SecurityCode,
            queryIsRegistered,
            processor == null ? throw new ArgumentNullException(nameof(processor)) : element => processor(element.ToObject<SecurityCodeContainer>().SecurityCode)
        )
        {
        }

        private RegistrationController(RegistrationType registrationType, QueryIsRegistered queryIsRegistered, Func<JsonElement, Task> processor)
        {
            this.RegistrationType = registrationType;
            this.QueryIsRegistered = queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered));
            this.Processor = processor;
        }

        public Func<JsonElement, Task> Processor { get; }

        public QueryIsRegistered QueryIsRegistered { get; }
        
        public RegistrationType RegistrationType { get; }

        private readonly struct SecurityCodeContainer
        {
            [JsonConstructor]
            public SecurityCodeContainer(string securityCode) => this.SecurityCode = securityCode;

            public string SecurityCode { get; }
        }
    }
}