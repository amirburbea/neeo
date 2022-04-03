namespace Neeo.Sdk;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
/// <param name="Success">A value indicating if the API call was successful.</param>
public record struct SuccessResponse(bool Success);
