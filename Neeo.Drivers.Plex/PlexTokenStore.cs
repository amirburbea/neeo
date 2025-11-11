using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Neeo.Drivers.Plex;

public interface IPlexTokenStore
{
    string? AuthToken { get; set; }

    string ClientIdentifier { get; }
}

internal sealed class PlexTokenStore : IPlexTokenStore
{
    private static readonly byte[] _key = PlexTokenStore.DeriveKey();

    private readonly IPlexSettingsManager _settingsManager;

    public PlexTokenStore(IPlexSettingsManager settingsManager)
    {
        this._settingsManager = settingsManager;
        if (settingsManager.HasFile(Constants.FileName) && TryDeserialize(settingsManager.ReadAllBytes(Constants.FileName)) is { } tokens)
        {
            (this.ClientIdentifier, this.AuthToken) = tokens;
        }
        else
        {
            this.ClientIdentifier = Guid.NewGuid().ToString("N");
            this.Serialize();
        }

        static PlexTokens? TryDeserialize(byte[] encryptedData)
        {
            ReadOnlySpan<byte> cipherText = encryptedData.AsSpan(Constants.IvLength + Constants.TagLength);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(cipherText.Length);
            try
            {
                Span<byte> decryptedBytes = buffer.AsSpan(0, cipherText.Length);
                using AesGcm aes = new(PlexTokenStore._key, Constants.TagLength);
                aes.Decrypt(
                    nonce: encryptedData.AsSpan(0, Constants.IvLength),
                    ciphertext: cipherText,
                    tag: encryptedData.AsSpan(Constants.IvLength, Constants.TagLength),
                    plaintext: decryptedBytes
                );
                return JsonSerializer.Deserialize<PlexTokens?>(decryptedBytes, JsonSerializerOptions.Web);
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    public string? AuthToken { get; private set; }

    public string ClientIdentifier { get; private set; }

    string? IPlexTokenStore.AuthToken
    {
        get => this.AuthToken;
        set
        {
            this.AuthToken = value;
            this.Serialize();
        }
    }

    private static byte[] DeriveKey()
    {
        byte[] output = new byte[32]; // 256 bits
        ReadOnlySpan<char> password = Environment.MachineName;
        ReadOnlySpan<char> saltChars = typeof(PlexTokenStore).FullName!;
        int maxSaltBytes = Encoding.UTF8.GetMaxByteCount(saltChars.Length);
        Span<byte> saltBytes = stackalloc byte[maxSaltBytes];
        int byteCount = Encoding.UTF8.GetBytes(saltChars, saltBytes);
        Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: saltBytes[..byteCount],
            destination: output,
            iterations: 50000,
            hashAlgorithm: HashAlgorithmName.SHA256
        );
        return output;
    }

    private void Serialize()
    {
        PlexTokens tokens = new(this.ClientIdentifier, this.AuthToken);
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(tokens, JsonSerializerOptions.Web);
        int size = Constants.IvLength + Constants.TagLength + jsonBytes.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            Span<byte> result = buffer.AsSpan(0, size);
            Span<byte> iv = result[..Constants.IvLength];
            RandomNumberGenerator.Fill(iv);
            using AesGcm aes = new(PlexTokenStore._key, Constants.TagLength);
            aes.Encrypt(
                nonce: iv,
                plaintext: jsonBytes,
                ciphertext: result[(Constants.IvLength + Constants.TagLength)..],
                tag: result.Slice(Constants.IvLength, Constants.TagLength)
            );
            this._settingsManager.WriteAllBytes(Constants.FileName, result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static class Constants
    {
        public const string FileName = "plex_auth.tokens";

        public const int IvLength = 12;

        public const int TagLength = 16;
    }

    private readonly record struct PlexTokens(string ClientIdentifier, string? AuthToken);
}
