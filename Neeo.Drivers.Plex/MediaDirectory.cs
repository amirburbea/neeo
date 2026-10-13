using System;

namespace Neeo.Drivers.Plex;

public readonly record struct MediaDirectory(
    string Title,
    int TotalSize,
    IMediaDirectoryItem[] Items,
    Uri? Thumbnail
);
