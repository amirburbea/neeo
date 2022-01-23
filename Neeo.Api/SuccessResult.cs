namespace Neeo.Api;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
public sealed record SuccessResult
{
    /// <summary>
    /// Gets or sets a value indicating if the API call was successful.
    /// </summary>
    public bool Success { get; init; } = true;
}