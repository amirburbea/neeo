namespace Neeo.Drivers.Plex;

public readonly struct PaginationParameters(int offset, int pageSize)
{
    public static readonly PaginationParameters Empty = default;

    public int Offset => offset;

    public int PageSize => pageSize;
}
