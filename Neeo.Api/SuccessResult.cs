namespace Neeo.Api;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
/// <param name="Success">A value indicating if the API call was successful.</param>
public record struct SuccessResult
{
    public bool Success { get; init; } = true;
}