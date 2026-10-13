using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public readonly record struct AlbumInfo(
    JsonElement Art,
    [property: JsonPropertyName("artist")] string[]? Artists,
    string? Thumbnail,
    string Title,
    int? Year,
    [property: JsonPropertyName("albumid")] int? AlbumId
) : IMediaInfo
{
    internal static readonly string[] Fields = ["thumbnail", "title", "year", "artist", "art"];

    string IMediaInfo.GetId() => $"albumid:{this.AlbumId}";

    public string GetCoverArt() => (
        this.Art.ValueKind == JsonValueKind.Object && this.Art.TryGetProperty("thumb", out JsonElement thumb)
            ? thumb.GetString()
            : this.Thumbnail ?? (this.Art.ValueKind == JsonValueKind.Object && this.Art.TryGetProperty("icon", out JsonElement icon) ? icon.GetString() : null)
    ) ?? string.Empty;

    public string GetDescription() => this.Artists is { Length: > 0 } artists ? string.Join(", ", artists) : this.Title;

    public string GetTitle() => this.Year is int year and > 0 ? $"{this.Title} ({year})" : this.Title;
}
