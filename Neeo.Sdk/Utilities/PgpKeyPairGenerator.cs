using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Neeo.Sdk.Utilities;

internal static class PgpKeyPairGenerator
{
    public static PgpKeyPair CreatePgpKeys(string? id = default)
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        RsaKeyPairGenerator generator = new();
        generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), random, 768, 8));
        AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();
        PgpSecretKey secretKey = new(PgpSignature.DefaultCertification, PublicKeyAlgorithmTag.RsaGeneral, pair.Public, pair.Private, DateTime.UtcNow, id ?? Guid.NewGuid().ToString(), SymmetricKeyAlgorithmTag.Aes256, passphrase, null, null, random);
        return new(secretKey.PublicKey, secretKey.ExtractPrivateKey(passphrase));
    }
}