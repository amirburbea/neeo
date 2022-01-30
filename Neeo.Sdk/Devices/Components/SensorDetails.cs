﻿using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Components;

[JsonInterfaceSerializationConverter(typeof(ISensorDetails))]
public interface ISensorDetails
{
    SensorType Type { get; }
}

internal record class SensorDetails(SensorType Type) : ISensorDetails
{
    public const string ComponentSuffix = "_SENSOR";
}