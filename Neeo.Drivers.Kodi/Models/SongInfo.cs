using System.Linq;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public record struct SongInfo(
    string? Album,
    [property: JsonPropertyName("artist")] string[] Artists,
    int Id,
    string Label,
    string Thumbnail,
    string Title,
    int? Track,
    [property: JsonPropertyName("songid")] int? SongId
) : IMediaInfo
{
    internal static readonly string[] Fields = { "album", "thumbnail", "title", "artist", "track" };

    string IMediaInfo.GetId() => $"songid:{this.SongId}";

    public string GetCoverArt() => this.Thumbnail;

    public string GetDescription() => string.Join(" - ", new[] { string.Join(", ", this.Artists), this.Album }.Where(static str => !string.IsNullOrEmpty(str)));

    public string GetTitle() => this.Track is int track ? $"{track}. {this.Label}" : this.Label;
}