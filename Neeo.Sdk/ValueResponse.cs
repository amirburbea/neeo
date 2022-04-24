namespace Neeo.Sdk;

/// <summary>
/// Simple container for a value in response to a REST request.
/// </summary>
/// <param name="Value">The value to contain.</param>
public readonly record struct ValueResponse(object Value);