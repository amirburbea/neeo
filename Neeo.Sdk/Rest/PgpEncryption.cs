using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Handles PGP encryption and key rotation for use with device registration.
/// </summary>
public interface IPgpEncryption
{
    /// <summary>
    /// Gets the textual representation of the current public key.
    /// </summary>
    string PublicKeyText { get; }

    /// <summary>
    /// Uses the current private key to decrypt the encrypted bytes.
    /// 
    /// In the event the encryption of the current bytes is not as expected, returns <c>null</c>.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted bytes to decrypt.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task<byte[]?> DecryptViaPrivateKeyAsync(string encryptedBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates new PGP keys.
    /// </summary>
    void RotateKeys();
}

internal sealed class PgpEncryption : IPgpEncryption
{
    private PgpKeyPair _keys = PgpEncryption.CreatePgpKeys();

    public string PublicKeyText => PgpEncryption.GetPublicKeyText(this._keys.PublicKey);

    public async Task<byte[]?> DecryptViaPrivateKeyAsync(string encryptedBytes, CancellationToken cancellationToken)
    {
        using MemoryStream inputStream = new(Encoding.UTF8.GetBytes(encryptedBytes));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject list = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject(); // There could be a wrapper.
        if (list is PgpEncryptedDataList and [PgpPublicKeyEncryptedData data, ..])
        {
            using Stream privateStream = data.GetDataStream(this._keys.PrivateKey);
            PgpObjectFactory privateFactory = new(privateStream);
            if (privateFactory.NextPgpObject() is PgpLiteralData literal)
            {
                using Stream credentialsStream = literal.GetInputStream();
                using MemoryStream outputStream = new();
                await credentialsStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                return outputStream.ToArray();
            }
        }
        return null;
    }

    public void RotateKeys() => this._keys = PgpEncryption.CreatePgpKeys();

    private static PgpKeyPair CreatePgpKeys()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        RsaKeyPairGenerator generator = new();
        generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), random, 768, 8));
        AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();
        PgpSecretKey secretKey = new(
            PgpSignature.DefaultCertification,
            PublicKeyAlgorithmTag.RsaGeneral,
            pair.Public,
            pair.Private,
            DateTime.UtcNow,
            Guid.NewGuid().ToString("N"),
            SymmetricKeyAlgorithmTag.Aes256,
            passphrase,
            null,
            null,
            random
        );
        return new(secretKey.PublicKey, secretKey.ExtractPrivateKey(passphrase));
    }

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
