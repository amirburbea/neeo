using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi.Models;

public record struct Filter(
    string Field,
    string Value,
    FilterOperator Operator = FilterOperator.Is
);

[JsonConverter(typeof(TextJsonConverter<FilterOperator>))]
public enum FilterOperator
{
    [Text("contains")]
    Contains,
    [Text("doesnotcontain")]
    DoesNotContain,
    [Text("is")]
    Is,
    [Text("isnot")]
    IsNot,
    [Text("startswith")]
    StartsWith,
    [Text("endswith")]
    EndsWith,
    [Text("greaterthan")]
    GreaterThan,
    [Text("lessthan")]
    LessThan,
    [Text("after")]
    After,
    [Text("before")]
    Before,
}