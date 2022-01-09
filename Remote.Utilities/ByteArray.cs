using System;
using System.Globalization;
using System.Text;

namespace Remote.Utilities;

public static class ByteArray
{
    public static byte[] FromHex(string hex)
    {
        ReadOnlySpan<char> span = hex.AsSpan();
        byte[] array = new byte[span.Length / 2];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = byte.Parse(span.Slice(i * 2, 2), NumberStyles.HexNumber);
        }
        return array;
    }

    public static string ToHex(byte[] bytes)
    {
        StringBuilder builder = new(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
