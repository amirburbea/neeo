using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device registration.
/// </summary>
public interface IRegistrationFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Registration;

    /// <summary>
    /// Asynchronously determine if the device is already registered. If the device was previously registered, then NEEO will not prompt for credentials.
    /// </summary>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> QueryIsRegisteredAsync();

    /// <summary>
    /// Given a stream containing PGP decrypted credentials, attempt to register the device.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    Task<RegistrationResult> RegisterAsync(Stream stream);
}

internal sealed class RegistrationFeature : IRegistrationFeature
{
    private readonly Func<Stream, Task<RegistrationResult>> _processor;
    private readonly QueryIsRegistered _queryIsRegistered;

    private RegistrationFeature(QueryIsRegistered queryIsRegistered, Func<Stream, Task<RegistrationResult>> processor)
    {
        (this._queryIsRegistered, this._processor) = (queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered)), processor);
    }

    public static RegistrationFeature Create<TPayload>(QueryIsRegistered queryIsRegistered, Func<TPayload, Task<RegistrationResult>> processor)
        where TPayload : struct
    {
        return new(queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered)), ProcessAsync);

        async Task<RegistrationResult> ProcessAsync(Stream stream)
        {
            TPayload payload = await JsonSerializer.DeserializeAsync<TPayload>(stream, JsonSerialization.Options).ConfigureAwait(false);
            return await processor(payload).ConfigureAwait(false);
        }
    }

    public Task<bool> QueryIsRegisteredAsync() => this._queryIsRegistered();

    public Task<RegistrationResult> RegisterAsync(Stream stream) => this._processor(stream);
}