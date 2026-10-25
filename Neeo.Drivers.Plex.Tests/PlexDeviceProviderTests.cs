using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Plex.Tests;

public sealed class PlexDeviceProviderTests : IDisposable
{
    private readonly string _tempDirectory;

    public PlexDeviceProviderTests()
    {
        this._tempDirectory = Path.Combine(Path.GetTempPath(), $"neeo_plex_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDirectory);
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
                // Ignore temp cleanup errors.
            }
        }
    }

    [Fact]
    public void RegisterImageUrl_ProducesShortDeterministicToken()
    {
        string rawUrl = "http://192.168.1.100:32400/photo/:/transcode?url=%2Flibrary%2Fmetadata%2F123%2Fthumb&width=480&height=480";
        string token1 = PlexDeviceProviderBase.RegisterImageUrl(rawUrl);
        string token2 = PlexDeviceProviderBase.RegisterImageUrl(rawUrl);

        Assert.Equal(12, token1.Length);
        Assert.Equal(token1, token2);
    }

    [Fact]
    public void ResolveImageUrl_ReturnsOriginalUrlForRegisteredToken()
    {
        string rawUrl = "http://192.168.1.100:32400/photo/:/transcode?url=test";
        string token = PlexDeviceProviderBase.RegisterImageUrl(rawUrl);
        string? resolved = PlexDeviceProviderBase.ResolveImageUrl(token);

        Assert.Equal(rawUrl, resolved);
    }

    [Fact]
    public void ResolveImageUrl_ReturnsNullForUnknownToken()
    {
        string? resolved = PlexDeviceProviderBase.ResolveImageUrl("nonexistent12");
        Assert.Null(resolved);
    }

    [Fact]
    public void PlexTokenStore_PersistsClientIdAndAuthToken_ViaSettingsStore()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("settingsPath", this._tempDirectory)])
            .Build();

        SettingsStoreProvider provider1 = new(config, NullLoggerFactory.Instance);
        IPlexTokenStore store1 = new PlexTokenStore(provider1.GetStore<PlexTokenStore>());
        Assert.False(string.IsNullOrEmpty(store1.ClientIdentifier));
        Assert.Null(store1.AuthToken);

        string initialClientId = store1.ClientIdentifier;
        store1.AuthToken = "plex-secret-token";

        SettingsStoreProvider provider2 = new(config, NullLoggerFactory.Instance);
        IPlexTokenStore store2 = new PlexTokenStore(provider2.GetStore<PlexTokenStore>());
        Assert.Equal(initialClientId, store2.ClientIdentifier);
        Assert.Equal("plex-secret-token", store2.AuthToken);

        store2.AuthToken = null;

        SettingsStoreProvider provider3 = new(config, NullLoggerFactory.Instance);
        IPlexTokenStore store3 = new PlexTokenStore(provider3.GetStore<PlexTokenStore>());
        Assert.Equal(initialClientId, store3.ClientIdentifier);
        Assert.Null(store3.AuthToken);
    }

    [Fact]
    public void PlexJsonSerializerOptions_SerializesAndDeserializesWireTypes()
    {
        PlayerData player = new("Living Room TV", "player-123", PlayerCapabilities.Playback | PlayerCapabilities.Timeline, "192.168.1.50", 32500, "Plex for Android", true);
        string json = JsonSerializer.Serialize(player, PlexJsonSerializerOptions.Default);
        PlayerData roundTripped = JsonSerializer.Deserialize<PlayerData>(json, PlexJsonSerializerOptions.Default);

        Assert.Equal(player, roundTripped);
    }
}
