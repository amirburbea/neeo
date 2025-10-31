using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Neeo.Drivers.PlexApi;

public interface IPlexTokenStore
{
    string? AuthToken { get; set; }

    string ClientIdentifier { get; }
}

internal sealed class PlexTokenStore : IPlexTokenStore
{
    private static readonly byte[] _key = PlexTokenStore.DeriveKey();

    private string? _authToken;
    private readonly IPlexSettingsManager _settingsManager;

    string? IPlexTokenStore.AuthToken
    {
        get => this._authToken;
        set
        {
            this._authToken = value;
            this.Serialize();
        }
    }

    public string ClientIdentifier { get; private set; }

    public PlexTokenStore(IPlexSettingsManager settingsManager)
    {
        this._settingsManager = settingsManager;
        if (!settingsManager.HasFile(Constants.FileName) || TryDeserialize(settingsManager.ReadAllBytes(Constants.FileName)) is not { } tokens)
        {
            this.ClientIdentifier = Guid.NewGuid().ToString("N");
            this.Serialize();
            return;
        }
        (this.ClientIdentifier, this._authToken) = tokens;

        static PlexTokens? TryDeserialize(byte[] encryptedData)
        {
            ReadOnlySpan<byte> cipherText = encryptedData.AsSpan(Constants.IvLength + Constants.TagLength);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(cipherText.Length);
            try
            {
                Span<byte> decryptedBytes = buffer.AsSpan(0, cipherText.Length);
                using (AesGcm aes = new(PlexTokenStore._key, Constants.TagLength))
                {
                    ReadOnlySpan<byte> iv = encryptedData.AsSpan(0, Constants.IvLength);
                    ReadOnlySpan<byte> tag = encryptedData.AsSpan(Constants.IvLength, Constants.TagLength);
                    aes.Decrypt(iv, cipherText, tag, decryptedBytes);
                }
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

    private static byte[] DeriveKey()
    {
        byte[] output = new byte[32]; // 256 bits
        ReadOnlySpan<char> passwordChars = Environment.MachineName;
        ReadOnlySpan<char> saltChars = typeof(PlexTokenStore).FullName!;
        int maxSaltBytes = Encoding.UTF8.GetMaxByteCount(saltChars.Length);
        Span<byte> saltBytes = stackalloc byte[maxSaltBytes];
        int actualSaltLength = Encoding.UTF8.GetBytes(saltChars, saltBytes);
        Rfc2898DeriveBytes.Pbkdf2(
            password: passwordChars,
            salt: saltBytes[..actualSaltLength],
            destination: output,
            iterations: 10000,
            hashAlgorithm: HashAlgorithmName.SHA256
        );
        return output;
    }

    private void Serialize()
    {
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(new PlexTokens(this.ClientIdentifier, this._authToken), JsonSerializerOptions.Web);
        int size = Constants.IvLength + Constants.TagLength + jsonBytes.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            Span<byte> result = buffer.AsSpan(0, size);
            Span<byte> ivSpan = result[..Constants.IvLength];
            RandomNumberGenerator.Fill(ivSpan);
            using (AesGcm aes = new(PlexTokenStore._key, Constants.TagLength))
            {
                aes.Encrypt(
                    ivSpan,
                    jsonBytes,
                    result[(Constants.IvLength + Constants.TagLength)..],
                    result.Slice(Constants.IvLength, Constants.TagLength)
                );
            }
            this._settingsManager.WriteAllBytes(Constants.FileName, result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static class Constants
    {
        public const string FileName = "plex_auth.json";

        public const int IvLength = 12;

        public const int TagLength = 16;
    }

    private readonly record struct PlexTokens(string ClientIdentifier, string? AuthToken);
}
