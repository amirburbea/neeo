using System;

namespace Neeo.Drivers.Plex;

public record struct LibrarySectionDetail(
    string Title,
    Uri Thumbnail
);
