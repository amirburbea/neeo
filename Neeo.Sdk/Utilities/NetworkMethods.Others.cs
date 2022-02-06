using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Neeo.Sdk.Utilities;

public static partial class NetworkMethods
{
    private static readonly Regex _arpLineRegex = new(@"^\? \((?<ip>(\d+\.){3}\d+)\).+(?<mac>([a-f\d]{2}:){5}[a-f\d]{2})", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    private static void PopulateNetworkDevicesViaCommandLine(Dictionary<IPAddress, PhysicalAddress> output)
    {
        using Process process = Process.Start(startInfo: new() { FileName = "arp", Arguments = "-an", UseShellExecute = false, RedirectStandardOutput = true })!;
        while (process.StandardOutput.ReadLine() is { } line)
        {
            if (line.Length > 0 && NetworkMethods._arpLineRegex.Match(line) is { Success: true } match)
            {
                output.Add(IPAddress.Parse(match.Groups["ip"].Value), PhysicalAddress.Parse(match.Groups["mac"].Value));
            }
        }
        process.WaitForExit();
    }
}