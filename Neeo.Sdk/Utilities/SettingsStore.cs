using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Default implementation of <see cref="ISettingsStoreProvider"/>.
/// </summary>
/// <param name="configuration">Configuration for settings path.</param>
/// <param name="loggerFactory">Logger factory.</param>
public sealed class SettingsStoreProvider(IConfiguration configuration, ILoggerFactory loggerFactory) : ISettingsStoreProvider
{
    private readonly ConcurrentDictionary<string, ISettingsStore> _stores = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SettingsStore> _logger = loggerFactory.CreateLogger<SettingsStore>();

    /// <summary>
    /// Gets the base directory on disk where encrypted settings files are stored.
    /// </summary>
    public string SettingsDirectory { get; } = SettingsStoreProvider.DetermineSettingsDirectory(configuration);

    /// <inheritdoc/>
    public ISettingsStore GetStore(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string fileName = SettingsStoreProvider.NormalizeFileName(name);
        return this._stores.GetOrAdd(fileName, file =>
        {
            string filePath = Path.Combine(this.SettingsDirectory, file);
            return new SettingsStore(file, filePath, this._logger);
        });
    }

    /// <inheritdoc/>
    public ISettingsStore<T> GetStore<T>() => new SettingsStore<T>(this);

    /// <summary>
    /// Resolves the store file name for the specified type, honoring <see cref="SettingsStoreAttribute"/> if present.
    /// </summary>
    /// <param name="type">The type to resolve the store name for.</param>
    /// <returns>The resolved file name.</returns>
    public static string ResolveStoreName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.GetCustomAttribute<SettingsStoreAttribute>() is { Name: { Length: > 0 } customName })
        {
            return customName;
        }
        return $"{type.Name.ToLowerInvariant()}.settings.bin";
    }

    private static string NormalizeFileName(string name)
    {
        if (name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }
        return $"{name}.settings.bin";
    }

    private static string DetermineSettingsDirectory(IConfiguration configuration)
    {
        string fallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neeo");
        string directory = Environment.GetEnvironmentVariable("NEEO_SETTINGS_PATH")
            ?? configuration["settingsPath"]
            ?? fallbackDirectory;
        if (!IsWritable(directory))
        {
            if (directory == fallbackDirectory || !IsWritable(fallbackDirectory))
            {
                throw new UnauthorizedAccessException($"The configured settings path '{directory}' is not writable.");
            }
            directory = fallbackDirectory;
        }
        return Path.GetFullPath(directory);

        static bool IsWritable(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                using (File.Create(Path.Combine(directoryPath, Path.GetRandomFileName()), bufferSize: 1, FileOptions.DeleteOnClose))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

/// <summary>
/// A persistent, encrypted key-value store using AES-GCM with a machine-derived key.
/// </summary>
internal sealed class SettingsStore : ISettingsStore
{
    private const int IvLength = 12;
    private const int TagLength = 16;

    private readonly Lock _lock = new();
    private readonly string _filePath;
    private readonly byte[] _key;
    private readonly ILogger<SettingsStore> _logger;
    private Dictionary<string, string> _entries = new(StringComparer.Ordinal);
    private int _batchDepth;

    public SettingsStore(string storeName, string filePath, ILogger<SettingsStore> logger)
    {
        this._filePath = filePath;
        this._key = SettingsStore.DeriveKey(storeName);
        this._logger = logger;
        this.Load();
    }

    public string this[string key]
    {
        get
        {
            lock (this._lock)
            {
                return this._entries[key];
            }
        }
        set
        {
            lock (this._lock)
            {
                this._entries[key] = value;
                this.Save();
            }
        }
    }

    public ICollection<string> Keys
    {
        get
        {
            lock (this._lock)
            {
                return [.. this._entries.Keys];
            }
        }
    }

    public ICollection<string> Values
    {
        get
        {
            lock (this._lock)
            {
                return [.. this._entries.Values];
            }
        }
    }

    public int Count
    {
        get
        {
            lock (this._lock)
            {
                return this._entries.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public string? Get(string key, string? defaultValue = default)
    {
        lock (this._lock)
        {
            return this._entries.TryGetValue(key, out string? value) ? value : defaultValue;
        }
    }

    public void Add(string key, string value)
    {
        lock (this._lock)
        {
            this._entries.Add(key, value);
            this.Save();
        }
    }

    public void Add(KeyValuePair<string, string> item) => this.Add(item.Key, item.Value);

    public void Clear()
    {
        lock (this._lock)
        {
            if (this._entries.Count == 0)
            {
                return;
            }
            this._entries.Clear();
            this.Save();
        }
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        lock (this._lock)
        {
            return ((ICollection<KeyValuePair<string, string>>)this._entries).Contains(item);
        }
    }

    public bool ContainsKey(string key)
    {
        lock (this._lock)
        {
            return this._entries.ContainsKey(key);
        }
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        lock (this._lock)
        {
            ((ICollection<KeyValuePair<string, string>>)this._entries).CopyTo(array, arrayIndex);
        }
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        lock (this._lock)
        {
            return new Dictionary<string, string>(this._entries).GetEnumerator();
        }
    }

    public bool Remove(string key)
    {
        lock (this._lock)
        {
            if (this._entries.Remove(key))
            {
                this.Save();
                return true;
            }
            return false;
        }
    }

    public bool Remove(KeyValuePair<string, string> item)
    {
        lock (this._lock)
        {
            if (((ICollection<KeyValuePair<string, string>>)this._entries).Remove(item))
            {
                this.Save();
                return true;
            }
            return false;
        }
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        lock (this._lock)
        {
            return this._entries.TryGetValue(key, out value);
        }
    }

    public void Update(Action<IDictionary<string, string>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (this._lock)
        {
            this._batchDepth++;
            try
            {
                action(this);
            }
            finally
            {
                this._batchDepth--;
                if (this._batchDepth == 0)
                {
                    this.Save();
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private void Load()
    {
        if (!File.Exists(this._filePath))
        {
            return;
        }
        try
        {
            byte[] encryptedData = File.ReadAllBytes(this._filePath);
            if (encryptedData.Length < IvLength + TagLength)
            {
                return;
            }
            ReadOnlySpan<byte> cipherText = encryptedData.AsSpan(IvLength + TagLength);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(cipherText.Length);
            try
            {
                Span<byte> decryptedBytes = buffer.AsSpan(0, cipherText.Length);
                using AesGcm decryptor = new(this._key, TagLength);
                decryptor.Decrypt(
                    nonce: encryptedData.AsSpan(0, IvLength),
                    ciphertext: cipherText,
                    tag: encryptedData.AsSpan(IvLength, TagLength),
                    plaintext: decryptedBytes
                );
                if (JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedBytes, AppJsonSerializerOptions.Default) is { } dict)
                {
                    this._entries = new(dict, StringComparer.Ordinal);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to decrypt settings from {path}; starting with fresh settings.", this._filePath);
        }
    }

    private void Save()
    {
        if (this._batchDepth > 0)
        {
            return;
        }
        try
        {
            string directory = Path.GetDirectoryName(this._filePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(this._entries, AppJsonSerializerOptions.Default);
            int totalSize = IvLength + TagLength + jsonBytes.Length;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                Span<byte> result = buffer.AsSpan(0, totalSize);
                Span<byte> iv = result[..IvLength];
                RandomNumberGenerator.Fill(iv);
                using AesGcm encryptor = new(this._key, TagLength);
                encryptor.Encrypt(
                    nonce: iv,
                    plaintext: jsonBytes,
                    ciphertext: result[(IvLength + TagLength)..],
                    tag: result.Slice(IvLength, TagLength)
                );
                string tempFile = $"{this._filePath}.{Guid.NewGuid():N}.tmp";
                File.WriteAllBytes(tempFile, buffer[..totalSize]);
                File.Move(tempFile, this._filePath, overwrite: true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to persist encrypted settings to {path}.", this._filePath);
        }
    }

    private static byte[] DeriveKey(string storeName)
    {
        byte[] output = new byte[32]; // 256 bits AES key
        ReadOnlySpan<char> password = Environment.MachineName;
        ReadOnlySpan<char> saltChars = string.Concat(typeof(SettingsStore).FullName, ".", storeName);
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
}

/// <summary>
/// Generic adapter wrapping <see cref="ISettingsStore"/> for type <typeparamref name="T"/>.
/// </summary>
internal sealed class SettingsStore<T>(ISettingsStoreProvider provider) : ISettingsStore<T>
{
    private readonly ISettingsStore _inner = provider.GetStore(SettingsStoreProvider.ResolveStoreName(typeof(T)));

    public string this[string key] { get => this._inner[key]; set => this._inner[key] = value; }
    public ICollection<string> Keys => this._inner.Keys;
    public ICollection<string> Values => this._inner.Values;
    public int Count => this._inner.Count;
    public bool IsReadOnly => this._inner.IsReadOnly;

    public void Add(string key, string value) => this._inner.Add(key, value);
    public void Add(KeyValuePair<string, string> item) => this._inner.Add(item);
    public void Clear() => this._inner.Clear();
    public bool Contains(KeyValuePair<string, string> item) => this._inner.Contains(item);
    public bool ContainsKey(string key) => this._inner.ContainsKey(key);
    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => this._inner.CopyTo(array, arrayIndex);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this._inner.GetEnumerator();
    public bool Remove(string key) => this._inner.Remove(key);
    public bool Remove(KeyValuePair<string, string> item) => this._inner.Remove(item);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => this._inner.TryGetValue(key, out value);
    public string? Get(string key, string? defaultValue = default) => this._inner.Get(key, defaultValue);
    public void Update(Action<IDictionary<string, string>> action) => this._inner.Update(action);
    IEnumerator IEnumerable.GetEnumerator() => this._inner.GetEnumerator();
}
