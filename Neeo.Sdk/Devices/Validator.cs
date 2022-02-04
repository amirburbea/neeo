using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Neeo.Sdk.Devices;

internal static class Validator
{
    [return: NotNullIfNotNull("value")]
    public static int? ValidateDelay(int? value, [CallerArgumentExpression("value")] string name = "") => value switch
    {
        < 0 => throw new ArgumentException($"Delay for {name} must not be negative.", name),
        > Constants.MaxDelay => throw new ArgumentException($"Delay for {name} must not exceed {Constants.MaxDelay}.", name),
        _ => value
    };

    public static int ValidateNotNegative(int value, [CallerArgumentExpression("value")] string name = "") => value < 0
        ? throw new ArgumentException($"Value for {name} must not be negative.", name)
        : value;

    public static int? ValidateNotNegative(int? value, [CallerArgumentExpression("value")] string name = "") => value.HasValue
        ? Validator.ValidateNotNegative(value.Value, name)
        : value;

    public static double[] ValidateRange(double low, double high) => double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || low >= high
        ? throw new ArgumentException("Range low must be less than range high and neither value can be infinity or NaN.")
        : new[] { low, high };

    [return: NotNullIfNotNull("text")]
    public static string? ValidateText(string? text, int minLength = 1, int maxLength = 48, bool allowNull = false, [CallerArgumentExpression("text")] string name = "") => text switch
    {
        null when !allowNull => throw new ArgumentException($"Value for {name} can not be null.", name),
        { Length: int length } when length < minLength || length > maxLength => throw new ArgumentException($"Value for {name} must be between {minLength} and {maxLength} characters long.", name),
        _ => text
    };

    private static class Constants
    {
        public const int MaxDelay = 60 * 1000;
    }
}
