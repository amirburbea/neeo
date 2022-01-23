using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Neeo.Api.Rest;

internal sealed class PgpComponents
{
    private readonly PgpPrivateKey _privateKey;

    public PgpComponents()
    {
        RsaKeyPairGenerator kpg = new();
        kpg.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new(), 768, 8));
        AsymmetricCipherKeyPair pair = kpg.GenerateKeyPair();
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        PgpSecretKey secretKey = new(
            PgpSignature.DefaultCertification,
            PublicKeyAlgorithmTag.RsaGeneral,
            pair.Public,
            pair.Private,
            DateTime.Now,
            Dns.GetHostName(),
            SymmetricKeyAlgorithmTag.Aes256,
            passphrase,
            default,
            default,
            random
        );
        this._privateKey = secretKey.ExtractPrivateKey(passphrase);
        this.PublicKeyArmored = ConvertToArmored(secretKey.PublicKey);
    }

    public string PublicKeyArmored { get; }

    public Stream Decrypt(string text)
    {
        using MemoryStream inputStream = new(Encoding.ASCII.GetBytes(text));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject root = inputFactory.NextPgpObject();
        PgpEncryptedDataList dataList = root is PgpEncryptedDataList list ? list : (PgpEncryptedDataList)inputFactory.NextPgpObject();
        PgpPublicKeyEncryptedData encyptedData = dataList.GetEncryptedDataObjects().Cast<PgpPublicKeyEncryptedData>().First();
        using Stream privateStream = encyptedData.GetDataStream(this._privateKey);
        PgpObjectFactory privateFactory = new(privateStream);
        return privateFactory.NextPgpObject() is not PgpLiteralData literal
            ? throw new ArgumentException("Text was not in the expected format.", nameof(text))
            : literal.GetInputStream();
    }

    private static string ConvertToArmored(PgpPublicKey publicKey)
    {
        using MemoryStream outputStream = new();
        using (ArmoredOutputStream armoredStream = new(outputStream))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, null);
            publicKey.Encode(armoredStream);
        }
        outputStream.Seek(0L, SeekOrigin.Begin);
        StreamReader reader = new(outputStream);
        return reader.ReadToEnd();
    }
}