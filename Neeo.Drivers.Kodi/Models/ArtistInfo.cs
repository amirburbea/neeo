using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public readonly record struct ArtistInfo(
    JsonElement Art,
    string Artist,
    string? Thumbnail,
    [property: JsonPropertyName("artistid")] int? ArtistId
) : IMediaInfo
{
    internal static readonly string[] Fields = ["thumbnail", "artist", "art"];

    string IMediaInfo.GetId() => $"artistid:{this.ArtistId}";

    public string GetCoverArt() => (
        this.Art.ValueKind == JsonValueKind.Object && this.Art.TryGetProperty("thumb", out JsonElement thumb)
            ? thumb.GetString()
            : this.Thumbnail ?? (this.Art.ValueKind == JsonValueKind.Object && this.Art.TryGetProperty("icon", out JsonElement icon) ? icon.GetString() : null)
    ) ?? string.Empty;

    public string GetDescription() => this.Artist;

    public string GetTitle() => this.Artist;
}
