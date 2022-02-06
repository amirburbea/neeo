using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

internal static class FlaggedEnumerations<T>
    where T : struct, Enum
{
    public static T? TryResolve(string name) => TextAttribute.GetEnum<T>(name);

    public static IEnumerable<string> GetNames(T flaggedValue)
    {
        ulong value = Unsafe.As<T, ulong>(ref flaggedValue);
        return (value & (value - 1ul)) == 0ul // Is this a single flag.
           ? new[] { TextAttribute.GetText(flaggedValue) }
           : ExtractFlags(value);
    }

    private static IEnumerable<string> ExtractFlags(ulong value)
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
