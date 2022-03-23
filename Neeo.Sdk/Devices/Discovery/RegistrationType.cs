using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Discovery;

/// <summary>
/// Registration types supported by NEEO.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<RegistrationType>))]
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