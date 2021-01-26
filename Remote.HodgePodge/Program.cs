using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Remote.Neeo;

namespace Remote.HodgePodge
{
    internal static class Program
    {
        static async Task Main()
        {
            var x = await BrainDiscovery.DiscoverBrainsAsync();
        }

        /*
        private static async Task Main()
        {
            using Device device = await DeviceDiscovery.DiscoverNextDevice();
            while (true)
            {
                Console.Write("Mode: (0 - Learn, 1 - Test, else quit): ");
                switch (Console.ReadLine())
                {
                    case "0":
                        await Program.LearnCodes(device);
                        break;
                    case "1":
                        await Program.TestCodes(device);
                        break;
                    default:
                        return;
                }
            }
        }

        private static async Task TestCodes(Device device)
        {
            string? fileName = Program.QueryFileName();
            if (fileName == null)
            {
                return;
            }

            Dictionary<string,string> dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName, Encoding.UTF8))!;
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
                await device.SendData(Program.GetBytes(text));
                await device.WaitForAck();
            }
        }

        private static async Task LearnCodes(Device device)
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
                dictionary[name] = Program.GetText(data);
            }
            File.WriteAllText(fileName, JsonSerializer.Serialize(dictionary), Encoding.UTF8);
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
            return Program.Query("What is the device name?") is string name
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $"commands_{name}.json")
                : null;
        }

        private static byte[] GetBytes(string text)
        {
            byte[] bytes = new byte[text.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(text.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return bytes;
        }

        public static string GetText(byte[] bytes)
        {
            StringBuilder builder = new(bytes.Length * 2);
            Array.ForEach(bytes, b => builder.Append(b.ToString("x2")));
            return builder.ToString();
        }
        */
    }
}
