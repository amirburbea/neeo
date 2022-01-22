using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Neeo.Api.Devices;


internal static class Validator
{
    public static void ValidateDelay(int? value, string name)
    {
        if (value is null)
        {
            return;
        }
        const int maxDelay = 60 * 1000;
        if (value is < 0 or > maxDelay)
        {
            throw new ValidationException($"{name} must be between 0 and {maxDelay}.");
        }
    }

    public static void ValidateRange(double low, double high)
    {
        if (double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || low >= high)
        {
            throw new ValidationException("Range low must be less than range high and neither value can be infinity or NaN.");
        }
    }

    [return: NotNullIfNotNull("text")]
    public static string? ValidateString(string? text, int minLength = 1, int maxLength = 48, bool allowNull = false, [CallerArgumentExpression("text")] string name = "")
    {
        return text switch
        {
            null when !allowNull => throw new ValidationException($"{name} is null."),
            { Length: int length } when length < minLength || length > maxLength => throw new ValidationException($"{name} must be between {minLength} and {maxLength} characters long."),
            _ => text
        };
    }
}
