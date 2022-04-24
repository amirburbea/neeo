namespace Neeo.Drivers.Kodi.Models;

public readonly record struct QueryData<TData>(int Total, TData[] Data);

internal static class QueryData
{
    public static QueryData<TData> Create<TData>(int total, TData[] data) => new(total, data);
}