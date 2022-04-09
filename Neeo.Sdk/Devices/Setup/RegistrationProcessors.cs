using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Setup;

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

/// <summary>
/// Represents the result of a registration attempt.
/// </summary>
public sealed class RegistrationResult
{
    /// <summary>
    /// Gets a registration result for a successful attempt.
    /// </summary>
    public static readonly RegistrationResult Success = new();

    private RegistrationResult(string? error = default) => this.Error = error;

    /// <summary>
    /// Gets the error encountered if the operation was not succcessful, (<see langword="null"/> if successful).
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets a value indicating if the registration attempt was successful.
    /// </summary>
    public bool IsSuccess => this.Error is null;

    /// <summary>
    /// Creates an instance of the <see cref="RegistrationResult"/> class with an error message.
    /// </summary>
    /// <param name="error">A message describing the error encountered in registration.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown if the value of <paramref name="error"/> is <see langword="null"/>.</exception>
    public static RegistrationResult Failed(string error) => new(error ?? throw new ArgumentNullException(error));
}