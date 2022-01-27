using System.Text.Json.Serialization;

namespace Neeo.Sdk;

/// <summary>
/// A structure containing a single value to return to the NEEO Brain.
/// </summary>
public readonly struct ValueResult
{
    /// <summary>
    /// Creates a new <see cref="ValueResult"/> with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to return to the NEEO Brain.</param>
    [JsonConstructor]
    public ValueResult(object value) => this.Value = value;

    /// <summary>
    /// Gets the value to return to the NEEO Brain.
    /// </summary>
    public object? Value { get; }
}