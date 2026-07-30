using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device registration.
/// </summary>
public interface IRegistrationFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Registration;

    /// <summary>
    /// Asynchronously determine if the device is already registered. If the device was previously
    /// registered, then NEEO will not prompt for credentials.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IsRegisteredResponse> QueryIsRegisteredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Given PGP encrypted credentials and a private key, attempt to register the device.
    /// </summary>
    /// <param name="jsonBytes">Credentials JSON bytes.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<RegistrationResult> RegisterAsync(byte[] jsonBytes, CancellationToken cancellationToken = default);
}

internal sealed class RegistrationFeature(QueryIsRegistered queryIsRegistered, Func<byte[], CancellationToken, Task<RegistrationResult>> register) : IRegistrationFeature
{
    public static RegistrationFeature Create<TPayload>(QueryIsRegistered queryIsRegistered, Func<TPayload, CancellationToken, Task<RegistrationResult>> register)
        where TPayload : struct
    {
        ArgumentNullException.ThrowIfNull(queryIsRegistered, nameof(queryIsRegistered));
        ArgumentNullException.ThrowIfNull(register, nameof(register));
        return new(
            queryIsRegistered,
            (utf8Bytes, cancellationToken) => register(
                JsonSerializer.Deserialize<TPayload>(utf8Bytes, AppJsonSerializerOptions.Default),
                cancellationToken
            )
        );
    }

    public async Task<IsRegisteredResponse> QueryIsRegisteredAsync(CancellationToken cancellationToken) => new(await queryIsRegistered(cancellationToken).ConfigureAwait(false));

    public Task<RegistrationResult> RegisterAsync(byte[] jsonBytes, CancellationToken cancellationToken) => register(jsonBytes, cancellationToken);
}
