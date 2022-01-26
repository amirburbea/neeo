using System.Text.Json.Serialization;

namespace Neeo.Api;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
public readonly struct SuccessResult
{
    ///// <summary>
    ///// Creates a new <see cref="SuccessResult"/> with a success value of <see langword="true" />.
    ///// </summary>
    public SuccessResult() => this.Success = true;

    /// <summary>
    /// Creates a new <see cref="SuccessResult"/> with the specified success value.
    /// </summary>
    /// <param name="success">The success value.</param>
    [JsonConstructor]
    public SuccessResult(bool success) => this.Success = success;

    public bool Success { get; }
}