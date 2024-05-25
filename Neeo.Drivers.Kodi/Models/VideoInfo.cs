using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public readonly record struct VideoInfo(
    JsonElement Art,
    [property: JsonPropertyName("genre")] string[]? Genres,
    string? Thumbnail,
    string Title,
    int? Year,
    [property: JsonPropertyName("movieid")] int? MovieId
) : IMediaInfo
{
    internal static readonly string[] Fields = ["thumbnail", "title", "year", "genre", "art"];

    string IMediaInfo.GetId() => $"movieid:{this.MovieId}";

    public string GetCoverArt() => (this.Art.TryGetProperty("poster", out JsonElement poster) ? poster.GetString() : this.Thumbnail ?? this.Art.GetProperty("icon").GetString())!;

    public string GetDescription() => this.Genres is { Length: > 0 } genres ? string.Join(", ", genres) : this.Title;

    public string GetTitle() => this.Year is int year ? $"{this.Title} ({year})" : this.Title;
}
