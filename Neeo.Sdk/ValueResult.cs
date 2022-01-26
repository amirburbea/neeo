using System.Text.Json.Serialization;

namespace Neeo.Sdk;

/// <summary>
/// A structure containing a single value to return to the NEEO Brain.
/// </summary>
public readonly struct ValueResult
{
    [JsonConstructor]
    public ValueResult(object value) => this.Value = value;

    public object? Value { get; }
}