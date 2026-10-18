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
    /// <summary>
    /// The name of the file (within <see cref="SettingsDirectory"/>) holding the randomly generated master key
    /// from which each store's encryption key is derived.
    /// </summary>
    public const string KeyFileName = "settings.key";

    private const int MasterKeyLength = 32;

    private readonly ILogger<SettingsStore> _logger = loggerFactory.CreateLogger<SettingsStore>();
    private readonly Lock _masterKeyLock = new();
    // Lazy so that concurrent first requests for a store construct (and load/migrate its file) exactly once.
    private readonly ConcurrentDictionary<string, Lazy<ISettingsStore>> _stores = new(StringComparer.OrdinalIgnoreCase);
    private byte[]? _masterKey;

    /// <summary>
    /// Gets the base directory on disk where encrypted settings files are stored.
    /// </summary>
    public string SettingsDirectory { get; } = SettingsStoreProvider.DetermineSettingsDirectory(configuration);

    /// <inheritdoc/>
    public ISettingsStore GetStore(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string fileName = SettingsStoreProvider.NormalizeFileName(name);
        Lazy<ISettingsStore> store = this._stores.GetOrAdd(fileName, file => new(() =>
        {
            string filePath = Path.Combine(this.SettingsDirectory, file);
            return new SettingsStore(file, filePath, SettingsStore.DeriveKey(this.GetMasterKey(), file), this._logger);
        }));
        try
        {
            return store.Value;
        }
        catch
        {
            // Don't cache the failure (Lazy would) - let a later call retry.
            this._stores.TryRemove(new(fileName, store));
            throw;
        }
    }

    /// <inheritdoc/>
    public ISettingsStore<T> GetStore<T>() where T : class => new SettingsStore<T>(this);

    /// <summary>
    /// Resolves the store file name for the specified type, honoring <see cref="SettingsStoreAttribute"/> if present.
    /// Otherwise the name is derived from the type's full name (so that identically named types in different
    /// drivers do not share a store).
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
        string fullName = (type.FullName ?? type.Name).Replace('+', '.');
        StringBuilder builder = new(fullName.Length);
        foreach (char c in fullName)
        {
            builder.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? char.ToLowerInvariant(c) : '_');
        }
        return builder.Append(".settings.bin").ToString();
    }

    private byte[] GetMasterKey()
    {
        using (this._masterKeyLock.EnterScope())
        {
            return this._masterKey ??= SettingsStoreProvider.LoadOrCreateMasterKey(Path.Combine(this.SettingsDirectory, KeyFileName));
        }
    }

    private static byte[] LoadOrCreateMasterKey(string keyFilePath)
    {
        if (!File.Exists(keyFilePath))
        {
            // Write to a temp file first and then move it into place (without overwriting), so a concurrent process
            // either wins the race or reads the winner's complete key file - never a partially written one.
            string tempFile = $"{keyFilePath}.{Guid.NewGuid():N}.tmp";
            FileStreamOptions options = new() { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }
            using (FileStream stream = new(tempFile, options))
            {
                stream.Write(RandomNumberGenerator.GetBytes(MasterKeyLength));
            }
            try
            {
                File.Move(tempFile, keyFilePath, overwrite: false);
            }
            catch (IOException) when (File.Exists(keyFilePath))
            {
                File.Delete(tempFile);
            }
        }
        byte[] key = File.ReadAllBytes(keyFilePath);
        if (key.Length != MasterKeyLength)
        {
            throw new InvalidDataException($"The settings key file '{keyFilePath}' is corrupt (expected {MasterKeyLength} bytes, found {key.Length}).");
        }
        return key;
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
/// A persistent, encrypted key-value store using AES-GCM, keyed from the provider's master key file.
/// The encryption keeps settings unreadable to someone who only has the store file; anyone who can also
/// read <see cref="SettingsStoreProvider.KeyFileName"/> can decrypt it.
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

    public SettingsStore(string storeName, string filePath, byte[] key, ILogger<SettingsStore> logger)
    {
        this._filePath = filePath;
        this._key = key;
        this._logger = logger;
        this.Load(storeName);
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

    private void Load(string storeName)
    {
        if (!File.Exists(this._filePath))
        {
            return;
        }
        byte[] encryptedData;
        try
        {
            encryptedData = File.ReadAllBytes(this._filePath);
        }
        catch (Exception ex)
        {
            // Don't fall through to "fresh settings" - the next save would overwrite a file we merely failed to read.
            throw new IOException($"Failed to read settings from '{this._filePath}'.", ex);
        }
        if (SettingsStore.TryDecrypt(this._key, encryptedData) is { } entries)
        {
            this._entries = entries;
            return;
        }
        if (SettingsStore.TryDecrypt(SettingsStore.DeriveLegacyKey(storeName), encryptedData) is { } legacyEntries)
        {
            // Written by an older version with a key derived from the machine name - re-encrypt with the current key.
            this._entries = legacyEntries;
            this._logger.LogInformation("Migrating settings at {path} to the settings key file.", this._filePath);
            this.Save();
            return;
        }
        // Keep the unreadable file rather than silently overwriting it on the next save.
        string backupPath = $"{this._filePath}.{DateTime.UtcNow:yyyyMMddHHmmss}.unreadable";
        try
        {
            File.Move(this._filePath, backupPath);
            this._logger.LogWarning("Failed to decrypt settings from {path}; moved it to {backupPath} and starting with fresh settings.", this._filePath, backupPath);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to decrypt settings from '{this._filePath}', and failed to move it aside to '{backupPath}'.", ex);
        }
    }

    internal static Dictionary<string, string>? TryDecrypt(byte[] key, byte[] encryptedData)
    {
        if (encryptedData.Length < IvLength + TagLength)
        {
            return null;
        }
        ReadOnlySpan<byte> cipherText = encryptedData.AsSpan(IvLength + TagLength);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(cipherText.Length);
        try
        {
            Span<byte> decryptedBytes = buffer.AsSpan(0, cipherText.Length);
            using AesGcm decryptor = new(key, TagLength);
            decryptor.Decrypt(
                nonce: encryptedData.AsSpan(0, IvLength),
                ciphertext: cipherText,
                tag: encryptedData.AsSpan(IvLength, TagLength),
                plaintext: decryptedBytes
            );
            return JsonSerializer.Deserialize(decryptedBytes, AppJsonSerializerContext.Default.DictionaryStringString) is { } dict
                ? new(dict, StringComparer.Ordinal)
                : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    internal static byte[] Encrypt(byte[] key, ReadOnlySpan<byte> plainText)
    {
        byte[] result = new byte[IvLength + TagLength + plainText.Length];
        Span<byte> iv = result.AsSpan(0, IvLength);
        RandomNumberGenerator.Fill(iv);
        using AesGcm encryptor = new(key, TagLength);
        encryptor.Encrypt(
            nonce: iv,
            plaintext: plainText,
            ciphertext: result.AsSpan(IvLength + TagLength),
            tag: result.AsSpan(IvLength, TagLength)
        );
        return result;
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
            byte[] encrypted = SettingsStore.Encrypt(this._key, JsonSerializer.SerializeToUtf8Bytes(this._entries, AppJsonSerializerContext.Default.DictionaryStringString));
            string tempFile = $"{this._filePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllBytes(tempFile, encrypted);
            File.Move(tempFile, this._filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to persist encrypted settings to {path}.", this._filePath);
        }
    }

    /// <summary>
    /// Derives the encryption key for a single store from the provider's master key.
    /// </summary>
    internal static byte[] DeriveKey(byte[] masterKey, string storeName) => HKDF.DeriveKey(
        HashAlgorithmName.SHA256,
        masterKey,
        outputLength: 32,
        salt: [],
        info: Encoding.UTF8.GetBytes(string.Concat(typeof(SettingsStore).FullName, ".", storeName))
    );

    /// <summary>
    /// The key used by earlier versions, derived only from the machine name. Kept solely to migrate existing files.
    /// </summary>
    internal static byte[] DeriveLegacyKey(string storeName)
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
internal sealed class SettingsStore<T>(ISettingsStoreProvider provider) : ISettingsStore<T> where T : class
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
