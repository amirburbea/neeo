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
            Validator.ValidateString(units, prefix: nameof(units));
            if (double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || low >= high)
            {
                throw new ValidationException("Range low must be less than range high and neither value can be infinity or NaN.");
            }
        }

        public static void ValidateString(string? text, int minLength = 1, int maxLength = 48, bool allowNull = false, [CallerMemberName] string prefix = null)
        {
            if (text == null)
            {
                if (allowNull)
                {
                    return;
                }
                throw new ValidationException($"{GetPrefix(prefix)} is null.");
            }
            if (text.Length < minLength)
            {
                throw new ValidationException($"{GetPrefix(prefix)} is too short (minimum is .{minLength}).");
            }
            if (text.Length > maxLength)
            {
                throw new ValidationException($"{GetPrefix(prefix)} is too long (maximum is {maxLength}).");
            }

            static string GetPrefix(string prefix) => prefix.StartsWith("Set") ? prefix[3..] : prefix;
        }
    }
}
