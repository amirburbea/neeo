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

namespace Neeo.Api.Rest;

internal sealed class PgpKeys
{
    public PgpKeys()
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
        (this.PublicKey, this.PrivateKey) = (secretKey.PublicKey, secretKey.ExtractPrivateKey(passphrase));
    }

    public PgpPrivateKey PrivateKey { get; }

    public PgpPublicKey PublicKey { get; }
}