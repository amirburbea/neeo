using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Neeo.Sdk;

/// <summary>
/// A structure indicating success - used as a standard return type for NEEO Brain APIs.
/// </summary>
internal readonly struct SuccessResponse : IEquatable<SuccessResponse>
{
    public SuccessResponse()
        : this(true)
    {
    }

    [JsonConstructor]
    public SuccessResponse(bool success) => this.Success = success;

    public bool Success { get; }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is SuccessResponse other && this.Equals(other);

    public bool Equals(SuccessResponse other) => other.Success == this.Success;

    public override int GetHashCode() => this.Success.GetHashCode();
}