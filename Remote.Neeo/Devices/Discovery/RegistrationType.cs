using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Discovery
{
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<RegistrationType>))]
    public enum RegistrationType
    {
        [Text("ACCOUNT")]
        Credentials,

        [Text("SECURITY_CODE")]
        SecurityCode,
    }
}
