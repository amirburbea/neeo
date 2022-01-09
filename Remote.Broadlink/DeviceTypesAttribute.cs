using System;

namespace Remote.Broadlink;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DeviceTypesAttribute : Attribute
{
    public DeviceTypesAttribute(params int[] deviceTypes) => this.DeviceTypes = deviceTypes;

    public int[] DeviceTypes { get; }
}
