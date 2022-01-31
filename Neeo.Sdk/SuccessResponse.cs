using System;
using System.Text.Json.Serialization;

namespace Neeo.Sdk;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
public readonly struct SuccessResponse
{
    internal static readonly Func<SuccessResponse, bool> SuccessProjection = response => response.Success;

    /// <summary>
    /// Creates a new <see cref="SuccessResponse"/> with the specified success value.
    /// </summary>
    /// <param name="success">A value indicating if the API call was successful.</param>
    [JsonConstructor]
    public SuccessResponse(bool success) => this.Success = success;

    /// <summary>
    /// A value indicating if the API call was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Implicit cast operator for <see cref="Boolean"/> values to <see cref="SuccessResponse"/>.
    /// </summary>
    /// <param name="success">A value indicating if the API call was successful.</param>
    public static implicit operator SuccessResponse(bool success) => new(success);
}