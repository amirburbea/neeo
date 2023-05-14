namespace Neeo.Sdk;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
public readonly struct SuccessResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessResponse"/> struct.
    /// </summary>
    public SuccessResponse()
    {
    }

    /// <summary>
    /// Returns <see langword="true" /> to indicate the operation was successful.
    /// </summary>
    public bool Success { get; } = true;
}