using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Discovery;

/// <summary>
/// Attempt to register a device adapter given a set of <paramref name="credentials"/>.
/// </summary>
/// <param name="credentials"></param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<RegistrationResult> CredentialsRegistrationProcessor(Credentials credentials);

/// <summary>
/// Attempt to register a device adapter given a security code.
/// </summary>
/// <param name="securityCode">The security code.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<RegistrationResult> SecurityCodeRegistrationProcessor(string securityCode);

/// <summary>
/// Simple authentication credentials with a username and password.
/// </summary>
public readonly struct Credentials
{
    /// <summary>
    /// Creates a new instance of <see cref="Credentials"/>.
    /// </summary>
    ///// <param name="userName">The identity of the user to use for authentication.</param>
    ///// <param name="password">The password to use for authentication.</param>
    [JsonConstructor]
    public Credentials(string userName, string password) => (this.UserName, this.Password) = (userName, password);

    /// <summary>
    /// Gets the password to use for authentication.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Gets the identity of the user to use for authentication.
    /// </summary>
    [JsonPropertyName("username")]
    public string UserName { get; }
}

public sealed class RegistrationResult
{
    public static readonly RegistrationResult Success = new();

    private RegistrationResult(string? error = default) => this.Error = error;

    public string? Error { get; }

    public bool IsFailed => this.Error != null;

    public static RegistrationResult Failed(string error) => new(error ?? throw new ArgumentNullException(error));
}