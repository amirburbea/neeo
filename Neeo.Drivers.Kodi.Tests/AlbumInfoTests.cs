using System.Text.Json;
using Neeo.Drivers.Kodi;
using Neeo.Drivers.Kodi.Models;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

public class AlbumInfoTests
{
    [Fact]
    public void Properties_And_Formatting_MatchExpected()
    {
        AlbumInfo album = new(
            Art: default,
            Artists: ["The Beatles"],
            Thumbnail: "image://album-thumb.jpg",
            Title: "Abbey Road",
            Year: 1969,
            AlbumId: 10
        );

        Assert.Equal("Abbey Road (1969)", album.GetTitle());
        Assert.Equal("The Beatles", album.GetDescription());
        Assert.Equal("image://album-thumb.jpg", album.GetCoverArt());
        Assert.Equal("albumid:10", ((IMediaInfo)album).GetId());
    }

    [Fact]
    public void Deserialization_PopulatesAlbumFields()
    {
        const string json = """
            {
                "albumid": 42,
                "title": "Dark Side of the Moon",
                "artist": ["Pink Floyd"],
                "year": 1973,
                "thumbnail": "image://pink-floyd.jpg",
                "art": { "thumb": "image://thumb.jpg" }
            }
            """;

        AlbumInfo album = JsonSerializer.Deserialize<AlbumInfo>(json, KodiJsonSerializerOptions.Default);

        Assert.Equal(42, album.AlbumId);
        Assert.Equal("Dark Side of the Moon", album.Title);
        Assert.NotNull(album.Artists);
        Assert.Equal(["Pink Floyd"], album.Artists);
        Assert.Equal(1973, album.Year);
        Assert.Equal("Dark Side of the Moon (1973)", album.GetTitle());
        Assert.Equal("Pink Floyd", album.GetDescription());
        Assert.Equal("image://thumb.jpg", album.GetCoverArt());
    }
}
