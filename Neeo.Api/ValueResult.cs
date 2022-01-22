namespace Neeo.Api;

/// <summary>
/// A structure containing a single value to return to the NEEO Brain.
/// </summary>
/// <param name="Value">The value to return to the NEEO Brain.</param>
public record struct ValueResult(object? Value);