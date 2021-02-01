using System.Net;
using System.Security.Cryptography;
using System.Text;
using Remote.Utilities;

namespace Remote.Neeo.Devices
{
    public static class UniqueName
    {
        public static string Generate(string root, string? prefix = null)
        {
            using SHA1 sha1 = SHA1.Create();
            byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes($"{prefix ?? Dns.GetHostName()}{root}"));
            return bytes.ToHex();
        }
    }
}
