using System;

namespace Neeo.Drivers.Plex;

public record struct MediaDirectory(
    string Title,
    int TotalSize,
    IMediaDirectoryItem[] Items,
    Uri? Thumbnail
);
