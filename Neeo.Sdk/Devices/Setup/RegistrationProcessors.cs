using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Setup;

/// <summary>
/// Attempt to register a device adapter given a <paramref name="userName"/> and <paramref name="password" />.
/// </summary>
/// <param name="userName">The user name.</param>
/// <param name="password">The password.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<RegistrationResult> CredentialsRegistrationProcessor(string userName, string password, CancellationToken cancellationToken = default);

/// <summary>
/// Attempt to register a device adapter given a security code.
/// </summary>
/// <param name="securityCode">The security code.</param>
/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
public delegate Task<RegistrationResult> SecurityCodeRegistrationProcessor(string securityCode, CancellationToken cancellationToken = default);

/// <summary>
/// Represents the result of a registration attempt.
/// </summary>
public readonly struct RegistrationResult(string? error = default)
{
    /// <summary>
    /// Gets a registration result for a successful attempt.
    /// </summary>
    public static readonly RegistrationResult Success = new();

    /// <summary>
    /// Gets the error encountered if the operation was not succcessful, (<see langword="null"/> if successful).
    /// </summary>
    public string? Error => error;

    /// <summary>
    /// Gets a value indicating if the registration attempt was successful.
    /// </summary>
    public bool IsSuccess => error is null;

    /// <summary>
    /// Creates an instance of the <see cref="RegistrationResult"/> class with an error message.
    /// </summary>
    /// <param name="error">A message describing the error encountered in registration.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown if the value of <paramref name="error"/> is <see langword="null"/>.</exception>
    public static RegistrationResult Failed(string error) => new(error ?? throw new ArgumentNullException(error));
}
