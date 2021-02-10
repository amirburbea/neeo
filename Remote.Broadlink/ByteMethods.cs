using System;
using System.Linq;

namespace Remote.Broadlink
{
    internal static class ByteMethods
    {
        public static int Checksum(this byte[] bytes) => bytes.Aggregate(0xbeaf, (sum, b) => sum + b) & 0xffff;

        /// <summary>
        /// Equivalent of JavaScript [...<paramref name="left"/>, ...<paramref name="right"/>].
        /// </summary>
        /// <returns>Combined byte array.</returns>
        public static byte[] Combine(this byte[] left, byte[] right)
        {
            byte[] output = new byte[left.Length + right.Length];
            Buffer.BlockCopy(left, 0, output, 0, left.Length);
            Buffer.BlockCopy(right, 0, output, left.Length, right.Length);
            return output;
        }
    }
}
