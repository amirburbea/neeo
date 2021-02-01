using System.Runtime.CompilerServices;

namespace Remote.Neeo.Devices
{
    internal static class Validator
    {
        public static void ValidateStringLength(string text, int minLength = 1, int maxLength = 48, [CallerMemberName] string prefix = null)
        {
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
