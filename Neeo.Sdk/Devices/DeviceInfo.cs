using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices;

public interface IDeviceInfo
{
    string Name { get; }

    [JsonPropertyName("specificname")]
    string? SpecificName { get; }

    IReadOnlyCollection<string> Tokens { get; }
}

internal readonly struct DeviceInfo : IDeviceInfo
{
    private readonly IDeviceAdapter _adapter;

    public DeviceInfo(IDeviceAdapter adapter) => this._adapter = adapter;

    public string Name => this._adapter.DeviceName;

    public string? SpecificName => this._adapter.SpecificName;

    public IReadOnlyCollection<string> Tokens => this._adapter.Tokens;
}