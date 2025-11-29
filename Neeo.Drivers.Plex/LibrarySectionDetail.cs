using System;

namespace Neeo.Drivers.Plex;

public readonly record struct LibrarySectionDetail(
    string Title,
    Uri Thumbnail
);
