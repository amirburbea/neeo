using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Json;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Neeo.Sdk.Rest;

public interface IPgpUtility
{
    string ArmoredPublicKey { get; }

    ValueTask<JsonElement> DeserializeEncryptedAsync(string armoredJson);
}

internal sealed class PgpUtility : IPgpUtility
{
    private readonly PgpPrivateKey _privateKey;

    public PgpUtility()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        RsaKeyPairGenerator kpg = new();
        kpg.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new(), 768, 8));
        AsymmetricCipherKeyPair pair = kpg.GenerateKeyPair();
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        PgpSecretKey secretKey = new(PgpSignature.DefaultCertification, PublicKeyAlgorithmTag.RsaGeneral, pair.Public, pair.Private, DateTime.Now, Dns.GetHostName(), SymmetricKeyAlgorithmTag.Aes256, passphrase, null, null, random);
        this._privateKey = secretKey.ExtractPrivateKey(passphrase);
        this.ArmoredPublicKey = PgpUtility.GetArmoredPublicKey(secretKey.PublicKey);
    }

    public string ArmoredPublicKey { get; }

    public async ValueTask<JsonElement> DeserializeEncryptedAsync(string armoredJson)
    {
        const string invalidTextError = "Invalid input text.";
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(armoredJson));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject pgpObj = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject();
        if (pgpObj is not PgpEncryptedDataList { Count: > 0 } list || list[0] is not PgpPublicKeyEncryptedData data)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream privateStream = data.GetDataStream(this._privateKey);
        PgpObjectFactory privateFactory = new(privateStream);
        if (privateFactory.NextPgpObject() is not PgpLiteralData literal)
        {
            throw new InvalidOperationException(invalidTextError);
        }
        using Stream stream = literal.GetInputStream();
        return (await JsonSerializer.DeserializeAsync<JsonElement>(stream, JsonSerialization.Options).ConfigureAwait(false))!;
    }

    private static string GetArmoredPublicKey(PgpPublicKey publicKey)
    {
        using MemoryStream outputStream = new();
        using (ArmoredOutputStream armoredStream = new(outputStream))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, null);
            publicKey.Encode(armoredStream);
        }
        outputStream.Seek(0L, SeekOrigin.Begin);
        using StreamReader reader = new(outputStream);
        return reader.ReadToEnd();
    }
}