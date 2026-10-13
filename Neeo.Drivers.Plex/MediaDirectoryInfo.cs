using System;

namespace Neeo.Drivers.Plex;

public readonly record struct MediaDirectoryInfo(
    string Title,
    int RatingKey,
    string? Key = null,
    Uri? Thumbnail = null
) : IMediaDirectoryItem
{
    MediaDirectoryItemType IMediaDirectoryItem.Type => MediaDirectoryItemType.Directory;
}
