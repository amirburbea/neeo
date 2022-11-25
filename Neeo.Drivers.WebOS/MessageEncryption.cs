using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Neeo.Drivers.WebOS;

internal static partial class MessageEncryption
{
    private static readonly byte[] _emptyIV = new byte[Constants.IVLength];

    private static readonly byte[] _salt =
    {
        0x63,
        0x61,
        0xb8,
        0x0e,
        0x9b,
        0xdc,
        0xa6,
        0x63,
        0x8d,
        0x07,
        0x20,
        0xf2,
        0xcc,
        0x56,
        0x8f,
        0xb9,
    };

    public static string Decrypt(byte[] key, byte[] encrypted)
    {
        byte[] DecryptBytes(ReadOnlySpan<byte> input, byte[]? iv = null)
        {
            using Aes aes = MessageEncryption.CreateAes(key, iv);
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream ms = new();
            ms.Write(input);
            ms.Position = 0;
            using CryptoStream cryptoStream = new(ms, decryptor, CryptoStreamMode.Read);
            byte[] output = new byte[input.Length];
            cryptoStream.Read(output);
            return output;
        }

        byte[] iv = DecryptBytes(encrypted.AsSpan(0, Constants.IVLength));
        byte[] decrypted = DecryptBytes(encrypted.AsSpan(Constants.IVLength), iv);
        int index = decrypted.AsSpan().IndexOf((byte)'\n');
        return Encoding.UTF8.GetString(index == -1 ? decrypted : decrypted.AsSpan(0, index));
    }

    public static byte[] DeriveKey(string keyCode) => keyCode is null || !MessageEncryption.KeyCodeRegex().IsMatch(keyCode)
        ? throw new ArgumentException("Invalid keyCode (must be 8 characters containing letters or numbers only).", nameof(keyCode))
        : KeyDerivation.Pbkdf2(
            keyCode,
            MessageEncryption._salt,
            KeyDerivationPrf.HMACSHA256,
            iterationCount: 16384,
            numBytesRequested: 16
        );

    public static byte[] Encrypt(byte[] key, string message)
    {
        byte[] EncryptBytes(ReadOnlySpan<byte> bytes, byte[]? iv = null)
        {
            using Aes aes = MessageEncryption.CreateAes(key, iv);
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using MemoryStream output = new();
            using CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes);
            return output.ToArray();
        }

        string PrepareMessage(string message)
        {
            const int messageBlockSize = 16;
            if (message.IndexOf('\r') != -1)
            {
                throw new ArgumentException("Invalid message (must not include the message terminator character \\r).", nameof(message));
            }
            message += '\r';
            if (message.Length % messageBlockSize == 0)
            {
                message += ' ';
            }
            if (message.Length % messageBlockSize is int remainder and not 0)
            {
                int padding = messageBlockSize - remainder;
                message += new string((char)padding, padding);
            }
            return message;
        }

        byte[] iv = RandomNumberGenerator.GetBytes(Constants.IVLength);
        byte[] encryptedIV = EncryptBytes(iv);
        byte[] encryptedData = EncryptBytes(Encoding.UTF8.GetBytes(PrepareMessage(message)), iv);
        byte[] output = new byte[encryptedIV.Length + encryptedData.Length];
        encryptedIV.AsSpan().CopyTo(output);
        encryptedData.AsSpan().CopyTo(output.AsSpan(encryptedIV.Length));
        return output;
    }

    private static Aes CreateAes(byte[] key, byte[]? iv)
    {
        Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.BlockSize = 128;
        aes.KeySize = 128;
        aes.IV = iv ?? MessageEncryption._emptyIV;
        aes.Key = key;
        return aes;
    }

    [GeneratedRegex(@"^[A-Za-z0-9]{8}$")]
    private static partial Regex KeyCodeRegex();

    private static class Constants
    {
        public const int IVLength = 16;
    }
}