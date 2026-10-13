using System.Text.Json;
using Neeo.Drivers.Kodi;
using Neeo.Drivers.Kodi.Models;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public class ArtistInfoTests
{
    [Fact]
    public void Properties_And_Formatting_MatchExpected()
    {
        ArtistInfo artist = new(
            Art: default,
            Artist: "Queen",
            Thumbnail: "image://queen.jpg",
            ArtistId: 7
        );

        Assert.Equal("Queen", artist.GetTitle());
        Assert.Equal("Queen", artist.GetDescription());
        Assert.Equal("image://queen.jpg", artist.GetCoverArt());
        Assert.Equal("artistid:7", ((IMediaInfo)artist).GetId());
    }

    [Fact]
    public void Deserialization_PopulatesArtistFields()
    {
        const string json = """
            {
                "artistid": 15,
                "artist": "Radiohead",
                "thumbnail": "image://radiohead.jpg",
                "art": { "thumb": "image://rh-thumb.jpg" }
            }
            """;

        ArtistInfo artist = JsonSerializer.Deserialize<ArtistInfo>(json, KodiJsonSerializerOptions.Default);

        Assert.Equal(15, artist.ArtistId);
        Assert.Equal("Radiohead", artist.Artist);
        Assert.Equal("Radiohead", artist.GetTitle());
        Assert.Equal("image://rh-thumb.jpg", artist.GetCoverArt());
    }
}
