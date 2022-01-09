using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices.Discovery;

/// <summary>
/// Struct containing a user name and password.
/// </summary>
public readonly struct Credentials
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Credentials"/> struct.
    /// </summary>
    /// <param name="username">The name of the user.</param>
    /// <param name="password">The password.</param>
    [JsonConstructor]
    public Credentials(string username, string password)
    {
        (this.UserName, this.Password) = (username, password);
    }

    /// <summary>
    /// The password.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    [JsonPropertyName("username")]
    public string UserName { get; }
}
