using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public readonly record struct LibrarySection(
    string Key,
    string Title,
    LibraryType Type
);

[JsonConverter(typeof(TextJsonConverter<LibraryType>))]
public enum LibraryType
{
    [Text("movie")]
    Movie,

    [Text("show")]
    Show,

    [Text("episode")]
    Episode,

    [Text("artist")]
    Artist,

    [Text("album")]
    Album,

    [Text("track")]
    Track,

    [Text("photo")]
    Photo,

    [Text("clip")]
    Clip,

    [Text("video")]
    Video,
}

internal static class LibraryTypeMethods
{
    public static MediaType GetMediaType(this LibraryType libraryType) => libraryType switch
    {
        LibraryType.Movie or LibraryType.Video => MediaType.Video,
        LibraryType.Episode or LibraryType.Show => MediaType.Episode,
        LibraryType.Album or LibraryType.Artist or LibraryType.Track => MediaType.Music,
        _ => MediaType.Photo,
    };
}
