namespace Neeo.Drivers.Plex;

public record struct LibrarySection(
    int Key,
    string Title,
    LibrarySectionType Type
);