using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neeo.Sdk.Utilities;

internal static class FlaggedEnumerations
{
    public static IEnumerable<string> GetNames<T>(T flaggedValue)
        where T : struct, Enum
    {
        return EnumNames<T>.GetNames(flaggedValue);
    }

    private static class EnumNames<T>
        where T : struct, Enum
    {
        private static readonly bool _isValidType = Enum.GetUnderlyingType(typeof(T)) == typeof(ulong);

        public static IEnumerable<string> GetNames(T value)
        {
            if (!EnumNames<T>._isValidType)
            {
                return Array.Empty<string>();
            }
            ulong numericValue = Unsafe.As<T, ulong>(ref value);
            return (numericValue & (numericValue - 1ul)) == 0ul // Is this a single flag.
               ? new[] { TextAttribute.GetText(value) }
               : ExtractFlags(numericValue);

            static IEnumerable<string> ExtractFlags(ulong value)
            {
                for (int bits = 0; bits < 64; bits++)
                {
                    ulong flag = 1ul << bits;
                    if (flag > value)
                    {
                        yield break;
                    }
                    if ((value & flag) == flag)
                    {
                        yield return TextAttribute.GetText(Unsafe.As<ulong, T>(ref flag));
                    }
                }
            }
        }
    }
}