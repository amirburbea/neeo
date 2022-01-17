using System;
using System.Buffers;

namespace Broadlink.RM;

internal static class ByteMethods
{
    public static int Checksum(this byte[] bytes)
    {
        int sum = 0xbeaf;
        foreach (byte b in bytes)
        {
            sum += b;
        }
        return sum & 0xffff;
    }

    /// <summary>
    /// Equivalent of JavaScript [...<paramref name="left"/>, ...<paramref name="right"/>].
    /// </summary>
    /// <returns>Combined byte array.</returns>
    public static byte[] Combine(this byte[] left, byte[] right)
    {
        byte[] output = new byte[left.Length + right.Length];
        left.CopyTo(output.AsSpan());
        right.CopyTo(output.AsSpan(left.Length));
        return output;
    }
}