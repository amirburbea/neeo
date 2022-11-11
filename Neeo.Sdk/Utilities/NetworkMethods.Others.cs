using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Neeo.Sdk.Utilities;

public static partial class NetworkMethods
{
    private static void PopulateNetworkDevicesViaCommandLine(Dictionary<IPAddress, PhysicalAddress> output)
    {
        using Process process = Process.Start(startInfo: new() { FileName = "arp", Arguments = "-an", UseShellExecute = false, RedirectStandardOutput = true })!;
        while (process.StandardOutput.ReadLine() is { } line)
        {
            if (line.Length > 0 && NetworkMethods.ArpLineRegex().Match(line) is { Success: true, Groups: { } groups })
            {
                output.Add(IPAddress.Parse(groups["ip"].Value), PhysicalAddress.Parse(groups["mac"].Value));
            }
        }
        process.WaitForExit();
    }

    [GeneratedRegex("^\\? \\((?<ip>(\\d+\\.){3}\\d+)\\).+(?<mac>([a-f\\d]{2}:){5}[a-f\\d]{2})", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "en-US")]
    private static partial Regex ArpLineRegex();
}