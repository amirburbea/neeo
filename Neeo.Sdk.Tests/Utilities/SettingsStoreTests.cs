using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly SettingsStoreProvider _provider;

    public SettingsStoreTests()
    {
        this._tempDirectory = Path.Combine(Path.GetTempPath(), $"neeo_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDirectory);
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("settingsPath", this._tempDirectory)])
            .Build();
        this._provider = new SettingsStoreProvider(config, NullLoggerFactory.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(this._tempDirectory))
        {
            try
            {
                Directory.Delete(this._tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in temp.
            }
        }
    }

    [Fact]
    public void Store_sets_and_gets_values_correctly()
    {
        ISettingsStore store = this._provider.GetStore("test_store");

        Assert.Null(store.Get("username"));
        Assert.False(store.TryGetValue("username", out _));

        store["username"] = "myuser";
        store["password"] = "mypassword";

        Assert.Equal("myuser", store["username"]);
        Assert.Equal("myuser", store.Get("username"));
        Assert.True(store.TryGetValue("password", out string? pass));
        Assert.Equal("mypassword", pass);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void Store_persists_across_instances_and_encrypts_file()
    {
        const string storeName = "secure_store.bin";
        ISettingsStore store1 = this._provider.GetStore(storeName);
        store1["secret_key"] = "super_secret_value_12345";

        string filePath = Path.Combine(this._tempDirectory, storeName);
        Assert.True(File.Exists(filePath));

        // Verify the file content on disk does not contain plaintext
        string rawFileText = File.ReadAllText(filePath, Encoding.Latin1);
        Assert.DoesNotContain("super_secret_value_12345", rawFileText);
        Assert.DoesNotContain("secret_key", rawFileText);

        // Create a new provider pointing to the same folder and read back
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("settingsPath", this._tempDirectory)])
            .Build();
        SettingsStoreProvider provider2 = new(config, NullLoggerFactory.Instance);
        ISettingsStore store2 = provider2.GetStore(storeName);

        Assert.Equal("super_secret_value_12345", store2.Get("secret_key"));
    }

    [Fact]
    public void Store_removes_and_clears_values()
    {
        ISettingsStore store = this._provider.GetStore("remove_test");
        store["a"] = "1";
        store["b"] = "2";

        Assert.True(store.Remove("a"));
        Assert.False(store.ContainsKey("a"));
        Assert.Equal("2", store["b"]);

        store.Clear();
        Assert.Empty(store);
    }

    [Fact]
    public void Update_batches_modifications()
    {
        ISettingsStore store = this._provider.GetStore("batch_test");
        store.Update(dict =>
        {
            dict["key1"] = "val1";
            dict["key2"] = "val2";
            dict["key3"] = "val3";
        });

        Assert.Equal("val1", store["key1"]);
        Assert.Equal("val2", store["key2"]);
        Assert.Equal("val3", store["key3"]);
    }

    [Fact]
    public void Generic_GetStore_uses_custom_attribute_or_type_name()
    {
        ISettingsStore<AttributedType> storeWithAttr = this._provider.GetStore<AttributedType>();
        storeWithAttr["custom"] = "value";

        string customFile = Path.Combine(this._tempDirectory, "custom_named_store.bin");
        Assert.True(File.Exists(customFile));

        ISettingsStore<PlainType> storePlain = this._provider.GetStore<PlainType>();
        storePlain["plain"] = "value";

        string plainFile = Path.Combine(this._tempDirectory, "neeo.sdk.tests.utilities.settingsstoretests.plaintype.settings.bin");
        Assert.True(File.Exists(plainFile));
    }

    [Fact]
    public void ResolveStoreName_distinguishes_same_named_types_in_different_namespaces()
    {
        Assert.NotEqual(
            SettingsStoreProvider.ResolveStoreName(typeof(DriverA.Settings)),
            SettingsStoreProvider.ResolveStoreName(typeof(DriverB.Settings))
        );
    }

    [Fact]
    public void Master_key_file_is_created_once_and_reused()
    {
        this._provider.GetStore("key_test")["a"] = "1";
        string keyFile = Path.Combine(this._tempDirectory, SettingsStoreProvider.KeyFileName);
        byte[] key = File.ReadAllBytes(keyFile);
        Assert.Equal(32, key.Length);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(keyFile));
        }

        SettingsStoreProvider provider2 = this.CreateProvider();
        Assert.Equal("1", provider2.GetStore("key_test").Get("a"));
        Assert.Equal(key, File.ReadAllBytes(keyFile));
    }

    [Fact]
    public void Unreadable_file_is_moved_aside_rather_than_overwritten()
    {
        this._provider.GetStore("first")["token"] = "secret";
        string firstFile = Path.Combine(this._tempDirectory, "first.settings.bin");
        string secondFile = Path.Combine(this._tempDirectory, "second.settings.bin");
        // Encrypted with first's key, so second can not decrypt it.
        File.Copy(firstFile, secondFile);
        byte[] original = File.ReadAllBytes(secondFile);

        ISettingsStore second = this.CreateProvider().GetStore("second");
        Assert.Empty(second);
        second["other"] = "x";

        string backup = Assert.Single(Directory.GetFiles(this._tempDirectory, "second.settings.bin.*.unreadable"));
        Assert.Equal(original, File.ReadAllBytes(backup));
    }

    [Fact]
    public void Files_written_with_the_legacy_machine_key_are_migrated()
    {
        const string storeName = "legacy.settings.bin";
        string filePath = Path.Combine(this._tempDirectory, storeName);
        byte[] legacy = SettingsStore.Encrypt(SettingsStore.DeriveLegacyKey(storeName), Encoding.UTF8.GetBytes("{\"authToken\":\"abc\"}"));
        File.WriteAllBytes(filePath, legacy);

        Assert.Equal("abc", this._provider.GetStore("legacy").Get("authToken"));
        // Re-encrypted with the key file's key: no longer readable with the legacy key, still readable by a new provider.
        Assert.Null(SettingsStore.TryDecrypt(SettingsStore.DeriveLegacyKey(storeName), File.ReadAllBytes(filePath)));
        Assert.Equal("abc", this.CreateProvider().GetStore("legacy").Get("authToken"));
    }

    private SettingsStoreProvider CreateProvider()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("settingsPath", this._tempDirectory)])
            .Build();
        return new(config, NullLoggerFactory.Instance);
    }

    [SettingsStore("custom_named_store.bin")]
    private sealed class AttributedType;

    private sealed class PlainType;

    private static class DriverA
    {
        public sealed class Settings;
    }

    private static class DriverB
    {
        public sealed class Settings;
    }
}

