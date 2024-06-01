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
    /// Given PGP encrypted credentials and a private key, attempt to register the device.
    /// </summary>
    /// <param name="credentials">The PGP encrypted credentials.</param>
    /// <param name="privateKey">The PGP private key.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<RegistrationResult> RegisterAsync(string credentials, PgpPrivateKey privateKey);
}

internal sealed class RegistrationFeature(QueryIsRegistered queryIsRegistered, Func<Stream, Task<RegistrationResult>> register) : IRegistrationFeature
{
    private readonly QueryIsRegistered _queryIsRegistered = queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered));

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
        PgpObject next = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject(); // There could be a wrapper.
        if (next is PgpEncryptedDataList { Count: not 0 } list && list[0] is PgpPublicKeyEncryptedData data)
        {
            using Stream privateStream = data.GetDataStream(privateKey);
            PgpObjectFactory privateFactory = new(privateStream);
            if (privateFactory.NextPgpObject() is PgpLiteralData literal)
            {
                using Stream credentialsStream = literal.GetInputStream();
                return await register(credentialsStream).ConfigureAwait(false);
            }
        }
        return RegistrationResult.Failed("Failed to decrypt credentials.");
    }
}
