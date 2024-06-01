using System.IO;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest;

internal sealed class PgpPublicKeyResponse(PgpKeyPair keyPair)
{
    [JsonPropertyName("publickey")]
    public string PublicKey { get; } = PgpPublicKeyResponse.GetPublicKeyText(keyPair.PublicKey);

    private static string GetPublicKeyText(PgpPublicKey publicKey)
    {
        using Stream outputStream = new MemoryStream();
        using (ArmoredOutputStream armoredStream = new(outputStream))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, default);
            publicKey.Encode(armoredStream);
        }
        outputStream.Seek(0L, SeekOrigin.Begin);
        using StreamReader reader = new(outputStream);
        return reader.ReadToEnd();
    }
}
