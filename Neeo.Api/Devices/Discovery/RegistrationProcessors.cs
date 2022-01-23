using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neeo.Api.Devices.Discovery;

/// <summary>
/// Attempt to register a device adapter given a security code.
/// </summary>
/// <param name="securityCode">The security code.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task SecurityCodeRegistrationProcessor(string securityCode);

/// <summary>
/// Simple authentication credentials with a username and password.
/// </summary>
/// <param name="UserName">The identity of the user to use for authentication.</param>
/// <param name="Password">The password to use for authentication.</param>
public sealed record class Credentials([property: JsonPropertyName("username")] string UserName, string Password);

/// <summary>
/// Attempt to register a device adapter given a set of <paramref name="credentials" />.
/// </summary>
/// <param name="credentials"></param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task CredentialsRegistrationProcessor(Credentials credentials);