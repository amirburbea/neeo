using System;
using System.Globalization;
using System.Text;

namespace Remote.Utilities
{
    public static class ByteArray
    {
        public static byte[] FromHex(string hex)
        {
            byte[] array = new byte[hex.Length / 2];
            var span = hex.AsSpan();
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = byte.Parse(span.Slice(i * 2, 2), NumberStyles.HexNumber);
            }
            return array;
        }

        public static string ToHex(this byte[] bytes)
        {
            StringBuilder builder = new(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
