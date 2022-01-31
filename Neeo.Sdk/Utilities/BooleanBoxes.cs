using System.Runtime.CompilerServices;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Static class containing the boxed boolean values to prevent excessive allocations.
/// </summary>
public static class BooleanBoxes
{
    /// <summary>
    /// A boxed <see cref="object"/> with a value of <see langword="false"/>.
    /// </summary>
    public static readonly object False = false;

    /// <summary>
    /// A boxed <see cref="object"/> with a value of <see langword="true"/>.
    /// </summary>
    public static readonly object True = true;

    /// <summary>
    /// Given the specified <paramref name="value"/>, gets the pre-allocated boxed value.
    /// </summary>
    /// <param name="value">The value for which to retrieve the boxed value.</param>
    /// <returns>A boxed version of the specified <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetBox(bool value) => value ? BooleanBoxes.True : BooleanBoxes.False;
}