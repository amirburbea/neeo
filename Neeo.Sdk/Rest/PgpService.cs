using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Utilities;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest;

public interface IPgpService
{
    string ArmoredPublicKey { get; }

    ValueTask<JsonElement> DeserializeEncryptedAsync(string armoredJson);
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

    public ValueTask<JsonElement> DeserializeEncryptedAsync(string armoredJson) => PgpMethods.DeserializeEncryptedAsync<JsonElement>(armoredJson, this._privateKey);
}