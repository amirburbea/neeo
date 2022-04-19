using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neeo.Sdk.Utilities;

internal static class FlaggedEnumerations<T>
    where T : struct, Enum
{
    private static readonly bool _isValidType = Enum.GetUnderlyingType(typeof(T)) == typeof(ulong);

    public static IEnumerable<string> GetNames(T flaggedValue)
    {
        if (!FlaggedEnumerations<T>._isValidType)
        {
            return Array.Empty<string>();
        }
        ulong value = Unsafe.As<T, ulong>(ref flaggedValue);
        return (value & (value - 1ul)) == 0ul // Is this a single flag.
           ? new[] { TextAttribute.GetText(flaggedValue) }
           : ExtractFlags(value);

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