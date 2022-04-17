namespace Neeo.Sdk;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
/// <param name="Success">A value indicating if the API call was successful.</param>
public record struct SuccessResponse(bool Success)
{
    /// <summary>
    /// Defines an implicit conversion wrapping a boolean value within a <see cref="SuccessResponse"/>.
    /// </summary>
    /// <param name="success">A value indicating if the API call was successful.</param>
    public static implicit operator SuccessResponse(bool success) => new(success);
}
