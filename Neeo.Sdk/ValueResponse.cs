using System.Text.Json.Serialization;

namespace Neeo.Sdk;

/// <summary>
/// A structure containing a single value to return to the NEEO Brain.
/// </summary>
public readonly struct ValueResponse
{
    /// <summary>
    /// Creates a new <see cref="ValueResponse"/> with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to return to the NEEO Brain.</param>
    [JsonConstructor]
    public ValueResponse(object value) => this.Value = value;

    /// <summary>
    /// Gets the value to return to the NEEO Brain.
    /// </summary>
    public object? Value { get; }
}