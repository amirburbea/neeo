using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public record struct EpisodeInfo(
    JsonElement Art,
    int? Episode,
    int? Season,
    [property: JsonPropertyName("showtitle")] string ShowTitle,
    string Title,
    [property: JsonPropertyName("episodeid")] int? EpisodeId
) : IMediaInfo
{
    internal static readonly string[] Fields = { "title", "showtitle", "season", "episode", "art" };

    string IMediaInfo.GetId() => $"episodeid:{this.EpisodeId}";

    public string GetCoverArt() => (this.Art.TryGetProperty("thumb", out JsonElement element) ? element.GetString() : this.Art.GetProperty("tvshow.poster").GetString())!;

    public string GetDescription() => this.Season is int season && this.Episode is int episode
        ? $"{this.ShowTitle} - S{season:00} E{episode:00}"
        : this.ShowTitle;

    public string GetTitle() => $"{this.ShowTitle} - {this.Title}";
}