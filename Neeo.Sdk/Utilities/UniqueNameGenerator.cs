using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Neeo.Sdk.Utilities;

internal static class UniqueNameGenerator
{
    public static string Generate(string root, string? prefix = null)
    {
        byte[] bytes = SHA1.HashData(Encoding.UTF8.GetBytes($"{prefix ?? Dns.GetHostName()}{root}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
