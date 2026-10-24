using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Neeo.Sdk.Utilities;

public static partial class NetworkMethods
{
    [GeneratedRegex("^\\? \\((?<ip>(\\d+\\.){3}\\d+)\\).+(?<mac>([a-f\\d]{2}:){5}[a-f\\d]{2})", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex ArpLineRegex();

    private const string ProcNetArpPath = "/proc/net/arp";

    /// <summary>
    /// Parses the contents of the Linux <c>/proc/net/arp</c> table, adding each complete entry to <paramref name="output"/>.
    /// </summary>
    internal static void ParseProcNetArp(IEnumerable<string> lines, Dictionary<IPAddress, PhysicalAddress> output)
    {
        foreach (string line in lines)
        {
            // Columns: IP address, HW type, Flags, HW address, Mask, Device (the first line is a header).
            string[] columns = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length < 4
                || !IPAddress.TryParse(columns[0], out IPAddress? address)
                || !PhysicalAddress.TryParse(columns[3].Replace(':', '-'), out PhysicalAddress? macAddress)
                // Flag 0x2 (ATF_COM) marks a completed entry, an unanswered one is listed with an all-zero address.
                || !int.TryParse(columns[2].AsSpan().TrimStart("0x"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int flags)
                || (flags & 0x2) == 0)
            {
                continue;
            }
            output[address] = macAddress;
        }
    }

    private static void PopulateNetworkDevicesViaCommandLine(Dictionary<IPAddress, PhysicalAddress> output)
    {
        if (File.Exists(NetworkMethods.ProcNetArpPath))
        {
            // Linux: read the kernel's table directly, which needs neither the arp binary nor a child process.
            NetworkMethods.ParseProcNetArp(File.ReadLines(NetworkMethods.ProcNetArpPath), output);
            return;
        }
        // macOS and other BSD-derived systems.
        using Process process = Process.Start(startInfo: new() { FileName = "arp", Arguments = "-an", UseShellExecute = false, RedirectStandardOutput = true })!;
        while (process.StandardOutput.ReadLine() is { } line)
        {
            if (line.Length > 0 && NetworkMethods.ArpLineRegex().Match(line) is { Success: true, Groups: { } groups })
            {
                output[IPAddress.Parse(groups["ip"].Value)] = PhysicalAddress.Parse(groups["mac"].Value);
            }
        }
        process.WaitForExit();
    }
}
