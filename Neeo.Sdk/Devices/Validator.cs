using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Neeo.Sdk.Devices;

internal static class Validator
{
    [return: NotNullIfNotNull("value")]
    public static int? ValidateDelay(
        int? value,
        [CallerArgumentExpression("value")] string name = ""
    ) => value is < 0 or > Constants.MaxDelay
        ? throw new ValidationException($"{name} must be less than or equal to {Constants.MaxDelay}.")
        : value;

    public static int ValidateNotNegative(
        int value,
        [CallerArgumentExpression("value")] string name = ""
    ) => value > 0 ? value : throw new ValidationException($"Value for {name} must not be negative.");

    public static int? ValidateNotNegative(
        int? value,
        [CallerArgumentExpression("value")] string name = ""
    ) => !value.HasValue ? null : Validator.ValidateNotNegative(value.Value, name);

    public static double[] ValidateRange(
            double low,
        double high
    ) => double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || low >= high
        ? throw new ValidationException("Range low must be less than range high and neither value can be infinity or NaN.")
        : new[] { low, high };

    [return: NotNullIfNotNull("text")]
    public static string? ValidateString(
        string? text,
        int minLength = 1,
        int maxLength = 48,
        bool allowNull = false,
        [CallerArgumentExpression("text")] string name = ""
    ) => text switch
    {
        null when !allowNull => throw new ValidationException($"Value for {name} can not be null."),
        { Length: int length } when length < minLength || length > maxLength => throw new ValidationException($"Value for {name} must be between {minLength} and {maxLength} characters long."),
        _ => text
    };


    private static class Constants
    {
        public const int MaxDelay = 60 * 1000;
    }
}

public sealed class ValidationException : Exception
{
    internal ValidationException(string message)
        : base(message)
    {
    }
}