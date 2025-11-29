using System;
using System.Buffers;
using System.IO;
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
    /// Uses the current private key to decrypt the encrypted text.
    ///
    /// In the event the encryption of the current bytes is not as expected, returns <c>null</c>.
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task<byte[]?> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default);
}

internal sealed class PgpEncryption : IPgpEncryption
{
    private PgpKeyPair Keys => field ??= CreatePgpKeys();

    public string PublicKeyText => field ??= PgpEncryption.GetPublicKeyText(this.Keys.PublicKey);

    public async Task<byte[]?> DecryptAsync(string encryptedText, CancellationToken cancellationToken)
    {
        using MemoryStream inputStream = new();
        int length = Encoding.UTF8.GetByteCount(encryptedText);
        byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Encoding.UTF8.GetBytes(encryptedText, bytes.AsSpan(0, length));
            await inputStream.WriteAsync(bytes.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
        inputStream.Position = 0L;
        using ArmoredInputStream armoredInputStream = new(inputStream);
        PgpObjectFactory inputFactory = new(armoredInputStream);
        while (inputFactory.NextPgpObject() is { } pgpObject)
        {
            if (pgpObject is PgpEncryptedDataList and [PgpPublicKeyEncryptedData data, ..])
            {
                return await DecryptAsync(this.Keys.PrivateKey, data, cancellationToken).ConfigureAwait(false);
            }
        }
        return null;

        static async Task<byte[]?> DecryptAsync(PgpPrivateKey privateKey, PgpPublicKeyEncryptedData data, CancellationToken cancellationToken)
        {
            using Stream privateStream = data.GetDataStream(privateKey);
            PgpObjectFactory privateFactory = new(privateStream);
            if (privateFactory.NextPgpObject() is not PgpLiteralData literal)
            {
                return null;
            }
            using Stream credentialsStream = literal.GetInputStream();
            using MemoryStream outputStream = new();
            await credentialsStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
            return outputStream.ToArray();
        }
    }

    private static PgpKeyPair CreatePgpKeys()
    {
        SecureRandom random = new();
        byte[] randomBytes = new byte[32];
        random.NextBytes(randomBytes);
        char[] passphrase = Convert.ToBase64String(randomBytes).ToCharArray();
        AsymmetricCipherKeyPair cipherKeyPair = CreateCipherKeyPair(random);
        PgpSecretKey secretKey = new(
            PgpSignature.DefaultCertification,
            PublicKeyAlgorithmTag.RsaGeneral,
            cipherKeyPair.Public,
            cipherKeyPair.Private,
            DateTime.UtcNow,
            "neeo",
            SymmetricKeyAlgorithmTag.Aes256,
            passphrase,
            null,
            null,
            random
        );
        return new(secretKey.PublicKey, secretKey.ExtractPrivateKey(passphrase));

        static AsymmetricCipherKeyPair CreateCipherKeyPair(SecureRandom random)
        {
            RsaKeyPairGenerator generator = new();
            // Due to NEEO's constraints, 512-bit RSA is sufficient for this local-network-only key pair.
            generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), random, 512, 4));
            return generator.GenerateKeyPair();
        }
    }

    private static string GetPublicKeyText(PgpPublicKey publicKey)
    {
        using StringWriter stringWriter = new();
        using (ArmoredOutputStream armoredStream = new(new StringWriterOutputStream(stringWriter)))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, default);
            publicKey.Encode(armoredStream);
        }
        return stringWriter.ToString();
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
