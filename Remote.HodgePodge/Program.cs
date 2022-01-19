using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Neeo.Api;
using Neeo.Api.Devices;
using Broadlink.RM;
using Neeo.Api.Devices.Discovery;

namespace Remote.HodgePodge;

internal static class Program
{
    private static async Task LearnCodes(RMDevice device)
    {
        string? fileName = Program.QueryFileName();
        if (fileName == null)
        {
            return;
        }
        Dictionary<string, string> dictionary = new();
        while (true)
        {
            if (Query("Command name?") is not string name)
            {
                break;
            }
            await device.BeginLearning();
            await device.WaitForAck();
            byte[] data = await device.WaitForData();
            dictionary[name] = Convert.ToHexString(data);
        }
        File.WriteAllText(fileName, JsonSerializer.Serialize(dictionary), Encoding.UTF8);
    }

    private static readonly Regex _ipAddressRegex = new(@"^\d+\.\d+\.\d+\.\d+$");

    private static async Task Main()
    {
        var arg = Environment.GetCommandLineArgs().LastOrDefault()?.Trim();

        Brain? brain;
        if (arg != null && _ipAddressRegex.IsMatch(arg))
        {
            IPAddress address = IPAddress.Parse(arg.Trim());
            brain = await Brain.CreateAsync(address);
        }
        else
        {
            Console.WriteLine("Discovering brain...");
            brain = await Brain.DiscoverAsync();
        }
        if (brain is null)
        {
            Console.Error.WriteLine("Brain not found.");
            return;
        }
        Console.WriteLine($"Brain found! {brain.IPAddress}");
        try
        {
            IDeviceBuilder builder = Device.CreateDevice("TV", DeviceType.TV)
                .SetManufacturer("Amir")
                .AddAdditionalSearchTokens("Naho")
                .AddButton("INPUT HDMI1")
                .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
                .AddTextLabel("A", "Label A", true, async (id) => await Task.FromResult(id))
                .RegisterFavoritesHandler((deviceId, favorite) => Task.CompletedTask)
                .AddSlider("Slider Name", "Slider Label", 0, 100, null, async (_) => await Task.FromResult(5d), (_, __) => Task.CompletedTask)
                //.EnableDiscovery(new("Header", "Description", false), (_) => Task.FromResult(Array.Empty<DiscoveryResult>()))
                .AddButtonHandler((deviceId, button) =>
                {
                    Console.WriteLine($"{deviceId}|{button}");
                    return Task.CompletedTask;
                });
            Console.WriteLine("Starting server...");
            await brain.StartServerAsync(new[] { builder });
            Console.WriteLine("Server started. Press any key to quit...   ");
            Console.ReadKey(true);
        }
        finally
        {
            Console.WriteLine("Server stopping...   ");
            await brain.StopServerAsync();
        }
    }

    private static async Task MainRM()
    {
        using RMDevice? remote = await RMDiscovery.DiscoverDeviceAsync();
        if (remote is null)
        {
            return;
        }
        while (true)
        {
            Console.Write("Mode: (0 - Learn, 1 - Test, else quit): ");
            switch (Console.ReadLine())
            {
                case "0":
                    await LearnCodes(remote);
                    break;

                case "1":
                    await TestCodes(remote);
                    break;

                default:
                    return;
            }
        }
    }

    private static string? Query(string prompt, string quitCommand = "Done")
    {
        Console.Write($"{prompt} ({quitCommand} to end) ");
        return Console.ReadLine() is string text && !text.Equals(quitCommand, StringComparison.OrdinalIgnoreCase)
            ? text
            : null;
    }

    private static string? QueryFileName()
    {
        return Query("What is the device name?") is string name
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $"commands_{name}.json")
            : null;
    }

    private static async Task TestCodes(RMDevice remote)
    {
        if (QueryFileName() is not { } fileName)
        {
            return;
        }
        Dictionary<string, string> dictionary = new(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName, Encoding.UTF8))!, StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            if (Program.Query("Command name?") is not string name)
            {
                return;
            }
            if (!dictionary.TryGetValue(name, out string? text))
            {
                Console.Error.WriteLine($"Command {name} not found");
                continue;
            }
            await remote.SendData(Convert.FromHexString(text));
            await remote.WaitForAck();
        }
    }
}