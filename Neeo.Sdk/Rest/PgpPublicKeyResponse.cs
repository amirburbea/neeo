using System.IO;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest;

internal sealed class PgpPublicKeyResponse
{
    public PgpPublicKeyResponse(PgpKeyPair pgpKeys)
    {
        using MemoryStream outputStream = new();
        using (ArmoredOutputStream armoredStream = new(outputStream))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, default);
            pgpKeys.PublicKey.Encode(armoredStream);
        }
        outputStream.Seek(0L, SeekOrigin.Begin);
        using StreamReader reader = new(outputStream);
        this.PublicKey = reader.ReadToEnd();
    }

    [JsonPropertyName("publickey")]
    public string PublicKey { get; }
}