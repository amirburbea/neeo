using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

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
    Task<IsRegisteredResponse> QueryIsRegisteredAsync();

    /// <summary>
    /// Given PGP encrypted credentials, attempt to register the device.
    /// </summary>
    /// <param name="credentials"></param>
    /// <param name="privateKey"></param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<RegistrationResult> RegisterAsync(string credentials, PgpPrivateKey privateKey);
}

internal sealed class RegistrationFeature : IRegistrationFeature
{
    private readonly QueryIsRegistered _queryIsRegistered;
    private readonly Func<Stream, Task<RegistrationResult>> _register;

    private RegistrationFeature(QueryIsRegistered queryIsRegistered, Func<Stream, Task<RegistrationResult>> register)
    {
        (this._queryIsRegistered, this._register) = (queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered)), register);
    }

    public static RegistrationFeature Create<TPayload>(QueryIsRegistered queryIsRegistered, Func<TPayload, Task<RegistrationResult>> register)
        where TPayload : struct
    {
        return new(queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered)), RegisterAsync);

        async Task<RegistrationResult> RegisterAsync(Stream stream)
        {
            TPayload payload = await JsonSerializer.DeserializeAsync<TPayload>(stream, JsonSerialization.Options).ConfigureAwait(false);
            return await register(payload).ConfigureAwait(false);
        }
    }

    public async Task<IsRegisteredResponse> QueryIsRegisteredAsync() => new(await this._queryIsRegistered().ConfigureAwait(false));

    public async Task<RegistrationResult> RegisterAsync(string credentials, PgpPrivateKey privateKey)
    {
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(credentials));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject next = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject();
        if (next is PgpEncryptedDataList { Count: > 0 } list && list[0] is PgpPublicKeyEncryptedData data)
        {
            using Stream privateStream = data.GetDataStream(privateKey);
            PgpObjectFactory privateFactory = new(privateStream);
            if (privateFactory.NextPgpObject() is PgpLiteralData literal)
            {
                using Stream credentialsStream = literal.GetInputStream();
                return await this._register(credentialsStream).ConfigureAwait(false);
            }
        }
        return RegistrationResult.Failed("Invalid credentials.");
    }
}