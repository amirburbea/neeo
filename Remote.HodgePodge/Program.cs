using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Remote.Broadlink;
using Remote.Neeo;
using Remote.Neeo.Devices;
using Remote.Utilities;

namespace Remote.HodgePodge;

internal static class Program
{
    private static byte[] ByteArrayFromHex(string hex)
    {
        ReadOnlySpan<char> span = hex.AsSpan();
        byte[] array = new byte[span.Length / 2];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = byte.Parse(span.Slice(i * 2, 2), NumberStyles.HexNumber);
        }
        return array;
    }

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
            dictionary[name] = ByteArray.ToHex(data);
        }
        File.WriteAllText(fileName, JsonSerializer.Serialize(dictionary), Encoding.UTF8);
    }

    private static async Task Main()
    {
        Console.WriteLine("Discovering brain...");
        var brain = await Brain.DiscoverAsync().ConfigureAwait(false);
        if (brain == null)
        {
            Console.Error.WriteLine("Brain not found.");
            return;
        }
        Console.WriteLine($"Brain found! {brain.IPAddress}");
        try
        {
            var builder = Device.Create("test", DeviceType.Accessory)
                .SetManufacturer("Manufacturer")
                .AddButtons(KnownButtons.PowerOn | KnownButtons.PowerOff)
                .AddButtonGroup(ButtonGroup.NumberPad)
                .AddButtons(KnownButtons.Menu)
                .AddButtonHandler((deviceId, button) =>
                {
                    Console.WriteLine($"{deviceId}|{button}");
                    return Task.CompletedTask;
                });
            Console.WriteLine("Starting server...");
            await brain.StartServerAsync("C#", new[] { builder }, port:9001);
            Console.WriteLine("Server started. Press any key to quit...   ");
            Console.ReadKey();
        }
        finally
        {
            await brain.StopServerAsync();
        }
    }

    private static async Task MainASRM()
    {
        using RMDevice? remote = await RMDiscovery.DiscoverDeviceAsync();
        if (remote == null)
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
        if(QueryFileName() is not { } fileName)
        {
            return;
        }

        Dictionary<string, string> dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName, Encoding.UTF8))!;
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
            await remote.SendData(ByteArrayFromHex(text));
            await remote.WaitForAck();
        }
    }
}