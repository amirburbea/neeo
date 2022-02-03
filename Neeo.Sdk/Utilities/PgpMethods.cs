using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Json;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Utilities;

internal static class PgpMethods
{
    public static async ValueTask<TValue> DeserializeEncryptedAsync<TValue>(string armoredJson, PgpPrivateKey privateKey)
    {
        const string invalidTextError = "Invalid input text.";
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(armoredJson ?? throw new ArgumentNullException(nameof(armoredJson))));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject pgpObj = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject();
        if (pgpObj is not PgpEncryptedDataList { Count: > 0 } list || list[0] is not PgpPublicKeyEncryptedData data)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream privateStream = data.GetDataStream(privateKey ?? throw new ArgumentNullException(nameof(privateKey)));
        PgpObjectFactory privateFactory = new(privateStream);
        if (privateFactory.NextPgpObject() is not PgpLiteralData literal)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream stream = literal.GetInputStream();
        return (await JsonSerializer.DeserializeAsync<TValue>(stream, JsonSerialization.Options).ConfigureAwait(false))!;
    }
}