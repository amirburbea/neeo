namespace Neeo.Api;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
/// <param name="Success">A value indicating if the API call was successful.</param>
public record struct SuccessResult(bool Success)
{
    /// <summary>
    /// An implicit cast from a boolean value to a <see cref="SuccessResult"/>.
    /// </summary>
    /// <param name="success">A value indicating if the API call was successful.</param>
    public static implicit operator SuccessResult(bool success) => new(success);
}