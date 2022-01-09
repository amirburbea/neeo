using System;
using System.Globalization;
using System.Text;

namespace Remote.Utilities;

public static class ByteArray
{
    

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
