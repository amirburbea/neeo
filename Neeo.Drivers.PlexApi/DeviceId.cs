using System;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.PlexApi;

internal readonly record struct DeviceId(string ServerName, DeviceType Type)
{
    public static explicit operator DeviceId(string deviceId)
    {
        int index = deviceId.IndexOf('¶');
        return index == -1 || !Enum.TryParse(deviceId[(index + 1)..], out DeviceType type)
            ? throw new ArgumentException("Invalid deviceId format.", nameof(deviceId))
            : new(deviceId[..index], type);
    }

    public static implicit operator string(DeviceId deviceId) => deviceId.ToString();

    public override string ToString() => $"{this.ServerName}¶{this.Type}";
}
