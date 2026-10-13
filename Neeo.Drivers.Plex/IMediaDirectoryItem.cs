using System;

namespace Neeo.Drivers.Plex;

public interface IMediaDirectoryItem
{
    MediaDirectoryItemType Type { get; }
}

public enum MediaDirectoryItemType
{
    Directory = 0,
    Item = 1,
}