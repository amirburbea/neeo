using System.Text;

namespace Neeo.Api.Utilities;

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
