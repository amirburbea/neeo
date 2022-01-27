using System;
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

namespace Neeo.Sdk.Rest;

public interface IPgpKeys
{
    PgpPrivateKey PrivateKey { get; }

    PgpPublicKey PublicKey { get; }
}

internal sealed record PgpKeys(PgpPublicKey PublicKey, PgpPrivateKey PrivateKey) : IPgpKeys
{
    public static PgpKeys Create()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        RsaKeyPairGenerator kpg = new();
        kpg.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new(), 768, 8));
        AsymmetricCipherKeyPair pair = kpg.GenerateKeyPair();
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        PgpSecretKey secretKey = new(PgpSignature.DefaultCertification, PublicKeyAlgorithmTag.RsaGeneral, pair.Public, pair.Private, DateTime.Now, Dns.GetHostName(), SymmetricKeyAlgorithmTag.Aes256, passphrase, null, null, random);
        return new(secretKey.PublicKey, secretKey.ExtractPrivateKey(passphrase));
    }
}