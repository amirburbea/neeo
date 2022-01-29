using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Utilities;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest;

public interface IPgpService
{
    string ArmoredPublicKey { get; }

    ValueTask<T> DeserializeEncryptedAsync<T>(string armoredJson);
}

internal sealed class PgpService : IPgpService
{
    private readonly PgpPrivateKey _privateKey;

    public PgpService()
    {
        PgpKeyPair pgpKeys = PgpMethods.CreatePgpKeys();
        (this._privateKey, this.ArmoredPublicKey) = (pgpKeys.PrivateKey, PgpMethods.GetArmoredPublicKey(pgpKeys.PublicKey));
    }

    public string ArmoredPublicKey { get; }

    public ValueTask<T> DeserializeEncryptedAsync<T>(string armoredJson) => PgpMethods.DeserializeEncryptedAsync<T>(armoredJson, this._privateKey);
}