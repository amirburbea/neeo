using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Discovery;

/// <summary>
/// Registration types supported by NEEO.
/// </summary>
[JsonConverter(typeof(TextAttribute.EnumJsonConverter<RegistrationType>))]
public enum RegistrationType
{
    /// <summary>
    /// Credentials (or Account) supports a username and password.
    /// </summary>
    [Text("ACCOUNT")]
    Credentials,

    /// <summary>
    /// Security code supports a single string of text.
    /// </summary>
    [Text("SECURITY_CODE")]
    SecurityCode,
}
