namespace Neeo.Sdk.Devices;

/// <summary>
/// Represents the result of a device search operation, including the matched device and its relevance scores.
/// </summary>
/// <param name="Item">The device model that was matched during the search.</param>
/// <param name="Score">The relevance score assigned to the matched device. Higher values indicate a better match.</param>
/// <param name="MaxScore">The maximum possible relevance score for the search context. Used to normalize or compare scores.</param>
public readonly record struct DeviceSearchResult(DeviceModel Item, double Score, double MaxScore);
