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
        if (value < 0 || value > maxDelay)
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
    public static string? ValidateString(string? text, int minLength = 1, int maxLength = 48, bool allowNull = false, [CallerMemberName] string name = "")
    {
        if (text == null && allowNull)
        {
            return null;
        }
        if (text is not { Length: int length })
        {
            throw new ValidationException($"{GetName()} is null.");
        }
        if (length < minLength || length > maxLength)
        {
            throw new ValidationException($"{GetName()} must be between {minLength} and {maxLength} characters long.");
        }
        return text;

        string GetName() => name.StartsWith("Set", StringComparison.Ordinal) ? name[3..] : name;
    }
}
