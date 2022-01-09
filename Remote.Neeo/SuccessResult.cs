namespace Remote.Neeo;

/// <summary>
/// A struct indicating success. Used a standard return type for NEEO Brain APIs.
/// </summary>
/// <param name="Success">A value indicating if the API call was successful.</param>
public record struct SuccessResult(bool Success);