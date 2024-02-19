namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Container for a value in response to a call to <see cref="IValueFeature.GetValueAsync" />.
/// </summary>
/// <param name="Value">The value to contain.</param>
public readonly record struct ValueResponse(object Value);
