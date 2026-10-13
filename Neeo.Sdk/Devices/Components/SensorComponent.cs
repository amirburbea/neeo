using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Components;

/// <summary>
/// Describes a device sensor.
/// </summary>
public sealed record class SensorComponent(
    string Name,
    string Label,
    string Path,
    SensorDetails Sensor
) : Component(ComponentType.Sensor, Name, Label, Path);

/// <summary>
/// Describes the details of a sensor.
/// </summary>
[JsonDerivedType(typeof(RangeSensorDetails))]
public record class SensorDetails(SensorType Type);

/// <summary>
/// Describes the details of a range sensor.
/// </summary>
public sealed record class RangeSensorDetails(
    IReadOnlyCollection<double> Range,
    string Unit
) : SensorDetails(SensorType.Range);
