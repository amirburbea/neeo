using System;
using System.Linq;
using System.Text.Json;
using Neeo.Sdk.Devices.Directories;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;

public sealed class DirectoryFeatureTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void AddEntry_serializes_to_expected_wire_shape()
    {
        DirectoryBuilder builder = new(new(BrowseIdentifier: "root"));
        builder.AddEntry(new DirectoryEntry("Movies", ThumbnailUri: "http://example.com/movies.png", BrowseIdentifier: ".movies"));

        DirectoryData data = builder.Build();
        JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize<IDirectoryItem>(data.Items.Single(), DirectoryFeatureTests._options));

        Assert.Equal("Movies", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(".movies", document.RootElement.GetProperty("browseIdentifier").GetString());
        Assert.Equal("http://example.com/movies.png", document.RootElement.GetProperty("thumbnailUri").GetString());
        Assert.False(document.RootElement.TryGetProperty("isButton", out _));
        Assert.False(document.RootElement.TryGetProperty("isTile", out _));
    }

    [Fact]
    public void AddEntry_with_relative_thumbnail_uri_throws()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        Assert.Throws<ArgumentException>(() => builder.AddEntry(new DirectoryEntry("Movies", ThumbnailUri: "not-a-uri")));
    }

    [Fact]
    public void AddTileRow_serializes_tiles_with_isTile_flag()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        builder.AddTileRow(new DirectoryTile("http://example.com/tile.png", ActionIdentifier: "TILE1"));

        DirectoryData data = builder.Build();
        JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize<IDirectoryItem>(data.Items.Single(), DirectoryFeatureTests._options));

        JsonElement tile = document.RootElement.GetProperty("tiles")[0];
        Assert.True(tile.GetProperty("isTile").GetBoolean());
        Assert.Equal("TILE1", tile.GetProperty("actionIdentifier").GetString());
    }

    [Fact]
    public void AddTileRow_without_thumbnail_uri_throws()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        Assert.Throws<ArgumentException>(() => builder.AddTileRow(new DirectoryTile("")));
    }

    [Fact]
    public void AddButtonRow_serializes_buttons_with_isButton_flag()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        builder.AddButtonRow(new DirectoryButton("Shuffle", Icon: DirectoryButtonIcon.Shuffle));

        DirectoryData data = builder.Build();
        JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize<IDirectoryItem>(data.Items.Single(), DirectoryFeatureTests._options));

        JsonElement button = document.RootElement.GetProperty("buttons")[0];
        Assert.True(button.GetProperty("isButton").GetBoolean());
        Assert.Equal("shuffle", button.GetProperty("iconName").GetString());
    }

    [Fact]
    public void AddButtonRow_with_empty_text_throws()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        Assert.Throws<ArgumentException>(() => builder.AddButtonRow(new DirectoryButton("")));
    }

    [Fact]
    public void AddInfoItem_serializes_with_isInfoItem_flag_and_no_uiAction()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        builder.AddInfoItem(new DirectoryInfoItem("Click me", "Are you sure?", AffirmativeButtonText: "Yes", NegativeButtonText: "No"));

        DirectoryData data = builder.Build();
        JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize<IDirectoryItem>(data.Items.Single(), DirectoryFeatureTests._options));

        Assert.True(document.RootElement.GetProperty("isInfoItem").GetBoolean());
        Assert.Equal("Are you sure?", document.RootElement.GetProperty("text").GetString());
        Assert.False(document.RootElement.TryGetProperty("uiAction", out _));
    }
}
