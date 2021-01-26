using System.Linq;

namespace Remote.Broadlink
{
    internal static class ByteMethods
    {
        public static int Checksum(this byte[] bytes) => bytes.Aggregate(0xbeaf, (sum, b) => sum + b) & 0xffff;
    }
}
