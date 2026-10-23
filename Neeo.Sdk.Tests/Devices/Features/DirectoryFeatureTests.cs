using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public void AddEntry_truncates_title_and_label_to_255_chars()
    {
        string longTitle = new('t', 300);
        string longLabel = new('l', 400);
        DirectoryBuilder builder = new(new(BrowseIdentifier: "root"));
        builder.AddEntry(new DirectoryEntry(longTitle, Label: longLabel, BrowseIdentifier: ".movies"));

        DirectoryData data = builder.Build();
        JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize<IDirectoryItem>(data.Items.Single(), DirectoryFeatureTests._options));

        Assert.Equal(new string('t', 255), document.RootElement.GetProperty("title").GetString());
        Assert.Equal(new string('l', 255), document.RootElement.GetProperty("label").GetString());
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
        Assert.Equal("Shuffle", button.GetProperty("title").GetString());
        Assert.False(button.TryGetProperty("text", out _));
        Assert.Equal("Shuffle", button.GetProperty("iconName").GetString());
    }

    [Fact]
    public void AddButtonRow_allows_icon_only_button_without_title()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        builder.AddButtonRow(new DirectoryButton(Icon: DirectoryButtonIcon.Repeat, ActionIdentifier: "REPEAT"));

        JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize<IDirectoryItem>(builder.Build().Items.Single(), DirectoryFeatureTests._options));

        JsonElement button = document.RootElement.GetProperty("buttons")[0];
        Assert.Equal("Repeat", button.GetProperty("iconName").GetString());
        Assert.False(button.TryGetProperty("title", out _));
    }

    [Theory]
    [InlineData(10, 64, 0, 10)] // Short first page: only the 10 earlier items.
    [InlineData(64, 64, 0, 64)]
    [InlineData(100, 64, 36, 64)]
    public void Metadata_previous_page_covers_only_the_items_before_the_offset(int offset, int limit, int previousOffset, int previousLimit)
    {
        DirectoryBuilder builder = new(new("root", limit, offset));
        builder.AddEntry(new DirectoryEntry("Entry"));

        DirectoryPageInfo? previous = builder.Build().Metadata.Previous;

        Assert.NotNull(previous);
        Assert.Equal(previousOffset, previous.Value.Offset);
        Assert.Equal(previousLimit, previous.Value.Limit);
    }

    [Fact]
    public void AddButtonRow_with_neither_text_nor_icon_throws()
    {
        DirectoryBuilder builder = new(new(Limit: 64));
        Assert.Throws<ArgumentException>(() => builder.AddButtonRow(new DirectoryButton("")));
        Assert.Throws<ArgumentException>(() => builder.AddButtonRow(new DirectoryButton()));
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

    [Fact]
    public void AddPageEntries_paginates_array_list_and_other_lists_and_sets_total_matching_items()
    {
        int[] sourceArray = [.. Enumerable.Range(0, 10)];
        List<int> sourceList = [.. sourceArray];
        ReadOnlyCollection<int> sourceReadOnly = new(sourceList);
        foreach (IReadOnlyList<int> source in (IReadOnlyList<int>[])[sourceArray, sourceList, sourceReadOnly])
        {
            List<int> projected = [];
            DirectoryBuilder builder = new(new(BrowseIdentifier: "root", Limit: 3, Offset: 4));
            builder.AddHeader("Header does not count toward EntryCount");
            builder.AddPageEntries(source, item =>
            {
                projected.Add(item);
                return new($"Item {item}", ActionIdentifier: $"id-{item}");
            });
            DirectoryData data = builder.Build();
            Assert.Equal([4, 5, 6], projected);
            Assert.Equal(3, builder.EntryCount);
            Assert.Equal(10, data.TotalMatchingItems);
            Assert.Equal(7, data.Metadata.Next?.Offset);
            Assert.Equal(1, data.Metadata.Previous?.Offset);
        }
    }

    [Theory]
    [InlineData(-5, 3, 3)]
    [InlineData(0, -1, 0)]
    [InlineData(25, 3, 0)]
    public void AddPageEntries_tolerates_out_of_range_parameters(int offset, int limit, int expectedCount)
    {
        DirectoryBuilder builder = new(new(BrowseIdentifier: "root", Limit: limit, Offset: offset));
        builder.AddPageEntries(Enumerable.Range(0, 10).ToArray(), item => new($"Item {item}", ActionIdentifier: $"id-{item}"));
        Assert.Equal(expectedCount, builder.EntryCount);
        Assert.Equal(10, builder.Build().TotalMatchingItems);
    }
}
