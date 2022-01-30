using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

public readonly struct BrowseParameters
{
    [JsonConstructor]
    public BrowseParameters(int limit, int? offset, string? browseIdentifier)
    {
        (this.Limit, this.Offset, this.BrowseIdentifier) = (limit, offset, browseIdentifier);
    }

    string? BrowseIdentifier { get; }

    int Limit { get; }

    int? Offset { get; }

    public void Deconstruct(out int? limit, out int? offset, out string? browseIdentifier)
    {
        (limit, offset, browseIdentifier) = (this.Limit, this.Offset, this.BrowseIdentifier);
    }
}

public delegate Task<IListBuilder> DeviceDirectoryBrowser(string deviceId, BrowseParameters parameters);