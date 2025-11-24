namespace Neeo.Drivers.Plex;

public record struct LibrarySection(
    int Key,
    string Title,
    LibrarySectionType Type
);

public enum LibrarySectionType
{
    Movie = 0,
    Show,
    Artist,
}
