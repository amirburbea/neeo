using System;
using System.Runtime.CompilerServices;

namespace Remote.Neeo.Devices
{
    internal static class Validator
    {
        public static void ValidateDelay(int? value, string name)
        {
            if (value is not int delay)
            {
                return;
            }
            const int maxDelay = 60 * 1000;
            if (delay < 0 || delay > maxDelay)
            {
                throw new ValidationException($"{name} must be between 0 and {maxDelay}.");
            }
        }

        public static void ValidateRange(double low, double high, string? units)
        {
            Validator.ValidateString(units, name: nameof(units), allowNull: true);
            if (double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || low >= high)
            {
                throw new ValidationException("Range low must be less than range high and neither value can be infinity or NaN.");
            }
        }

        public static void ValidateString(string? text, int minLength = 1, int maxLength = 48, bool allowNull = false, [CallerMemberName] string name = "")
        {
            if (text == null)
            {
                if (allowNull)
                {
                    return;
                }
                throw new ValidationException($"{GetName()} is null.");
            }
            if (text.Length < minLength)
            {
                throw new ValidationException($"{GetName()} is too short (minimum is .{minLength}).");
            }
            if (text.Length > maxLength)
            {
                throw new ValidationException($"{GetName()} is too long (maximum is {maxLength}).");
            }

            string GetName() => name.StartsWith("Set", StringComparison.Ordinal) ? name[3..] : name;
        }
    }
}
