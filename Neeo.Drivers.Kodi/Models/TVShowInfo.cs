using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Kodi.Models;

public record struct TVShowInfo(string Label, JsonElement Art, int Year, [property: JsonPropertyName("tvshowid")] int TVShowId) : IMediaInfo
{
    internal static readonly string[] Fields = { "art", "year" };

    string IMediaInfo.GetId() => $"tvshowid:{this.TVShowId}";

    public string GetCoverArt() => (this.Art.TryGetProperty("poster", out JsonElement poster) ? poster.GetString() : this.Art.GetProperty("icon").GetString())!;

    public string GetDescription() => $"{this.Label} ({this.Year})";

    public string GetTitle() => this.Label;
}