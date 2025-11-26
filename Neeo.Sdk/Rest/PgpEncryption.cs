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
    private readonly Lock _lock = new();
    private PgpKeyPair? _keyPair;
    public string PublicKeyText => PgpEncryption.GetPublicKeyText(this.GetKeyPair().PublicKey);

    public async Task<byte[]?> DecryptViaPrivateKeyAsync(string encryptedBytes, CancellationToken cancellationToken)
    {
        using MemoryStream inputStream = new(Encoding.UTF8.GetBytes(encryptedBytes));
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        PgpObject list = inputFactory.NextPgpObject() as PgpEncryptedDataList ?? inputFactory.NextPgpObject(); // There could be a wrapper.
        if (list is PgpEncryptedDataList and [PgpPublicKeyEncryptedData data, ..])
        {
            using Stream privateStream = data.GetDataStream(this.GetKeyPair().PrivateKey);
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

    public void RotateKeys()
    {
        using (this._lock.EnterScope())
        {
            this._keyPair = PgpEncryption.CreatePgpKeys();
        }
    }

    private static PgpKeyPair CreatePgpKeys()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
        char[] passphrase = Encoding.ASCII.GetChars(randomBytes);
        SecureRandom random = new();
        random.SetSeed(randomBytes);
        RsaKeyPairGenerator generator = new();
        // NEEO Brain requires PGP for device registration. Since we rotate keys after each use,
        // 512-bit RSA is sufficient for this temporary, local-network-only use case.
        generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), random, 512, 4));
        AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();
        PgpSecretKey secretKey = new(
            PgpSignature.DefaultCertification,
            PublicKeyAlgorithmTag.RsaGeneral,
            pair.Public,
            pair.Private,
            DateTime.UtcNow,
            "neeo",
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
        using StringWriter stringWriter = new();
        using (ArmoredOutputStream armoredStream = new (new StringWriterOutputStream(stringWriter)))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, default);
            publicKey.Encode(armoredStream);
        }
        return stringWriter.ToString();
    }

    private PgpKeyPair GetKeyPair()
    {
        if (this._keyPair is { } keys)
        {
            return keys;
        }
        using (this._lock.EnterScope())
        {
            return this._keyPair ??= PgpEncryption.CreatePgpKeys();
        }
    }

    private sealed class StringWriterOutputStream(StringWriter writer) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => writer.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            ReadOnlySpan<byte> bytes = buffer.AsSpan(offset, count);
            Span<char> characters = stackalloc char[count];
            int length = Encoding.ASCII.GetChars(bytes, characters);
            writer.Write(characters[..length]);
        }
    }
}
