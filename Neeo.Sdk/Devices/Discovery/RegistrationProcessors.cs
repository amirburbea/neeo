using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Discovery;

/// <summary>
/// Attempt to register a device adapter given a <paramref name="userName"/> and <paramref name="password" />.
/// </summary>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<RegistrationResult> CredentialsRegistrationProcessor(string userName, string password);

/// <summary>
/// Attempt to register a device adapter given a security code.
/// </summary>
/// <param name="securityCode">The security code.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<RegistrationResult> SecurityCodeRegistrationProcessor(string securityCode);

public sealed class RegistrationResult
{
    public static readonly RegistrationResult Success = new();

    private RegistrationResult(string? error = default) => this.Error = error;

    public string? Error { get; }

    public bool IsFailed => this.Error != null;

    public static RegistrationResult Failed(string error) => new(error ?? throw new ArgumentNullException(error));
}