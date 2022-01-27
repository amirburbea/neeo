using System.Text.Json.Serialization;

namespace Neeo.Sdk;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
public readonly struct SuccessResult
{
    /// <summary>
    /// Creates a new <see cref="SuccessResult"/> with a success value of <see langword="true" />.
    /// </summary>
    public SuccessResult() => this.Success = true;

    /// <summary>
    /// Creates a new <see cref="SuccessResult"/> with the specified success value.
    /// </summary>
    /// <param name="success">A value indicating if the API call was successful.</param>
    [JsonConstructor]
    public SuccessResult(bool success) => this.Success = success;

    /// <summary>
    /// A value indicating if the API call was successful.
    /// </summary>
    public bool Success { get; }
}