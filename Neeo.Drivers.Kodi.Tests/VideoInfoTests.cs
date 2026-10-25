using System.Text.Json;
using Neeo.Drivers.Kodi.Models;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

public class VideoInfoTests
{
    [Fact]
    public void GetDescription_ReturnsPlot_WhenPlotIsPresent()
    {
        VideoInfo video = new(
            Art: default,
            Genres: ["Action", "Sci-Fi"],
            Plot: "A mind-bending thriller about dreams within dreams.",
            Thumbnail: null,
            Title: "Inception",
            Year: 2010,
            MovieId: 1
        );

        Assert.Equal("A mind-bending thriller about dreams within dreams.", video.GetDescription());
    }

    [Fact]
    public void GetDescription_FallsBackToGenres_WhenPlotIsEmpty()
    {
        VideoInfo video = new(
            Art: default,
            Genres: ["Comedy", "Drama"],
            Plot: "",
            Thumbnail: null,
            Title: "A Comedy Drama",
            Year: 2015,
            MovieId: 3
        );

        Assert.Equal("Comedy, Drama", video.GetDescription());
    }

    [Fact]
    public void GetDescription_FallsBackToTitle_WhenPlotAndGenresAreEmpty()
    {
        VideoInfo video = new(
            Art: default,
            Genres: [],
            Plot: null,
            Thumbnail: null,
            Title: "Mystery Movie",
            Year: 2022,
            MovieId: 4
        );

        Assert.Equal("Mystery Movie", video.GetDescription());
    }

    [Fact]
    public void Deserialization_PopulatesPlot()
    {
        const string json = """
            {
                "movieid": 42,
                "title": "The Matrix",
                "year": 1999,
                "plot": "A computer hacker learns the truth about reality.",
                "genre": ["Action", "Sci-Fi"],
                "thumbnail": "image://thumb.jpg",
                "art": {}
            }
            """;

        VideoInfo video = JsonSerializer.Deserialize<VideoInfo>(json, KodiJsonSerializerOptions.Default);

        Assert.Equal("The Matrix", video.Title);
        Assert.Equal(1999, video.Year);
        Assert.Equal(42, video.MovieId);
        Assert.Equal("A computer hacker learns the truth about reality.", video.Plot);
        Assert.Equal("A computer hacker learns the truth about reality.", video.GetDescription());
    }
}
