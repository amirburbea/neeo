using System;

namespace Neeo.Drivers.Plex;

public readonly record struct MediaItem(
    int RatingKey,
    MediaType Type,
    string Title,
    string? Summary = null,
    Uri? ThumbnailUri = null
) : IMediaDirectoryItem
{
    MediaDirectoryItemType IMediaDirectoryItem.Type => MediaDirectoryItemType.Item;
}
