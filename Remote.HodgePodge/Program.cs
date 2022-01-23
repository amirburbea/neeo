using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Broadlink.RM;
using Neeo.Api;
using Neeo.Api.Devices;
using Neeo.Discovery;

namespace Remote.HodgePodge;

internal static class Program
{
    private static readonly Regex _ipAddressRegex = new(@"^\d+\.\d+\.\d+\.\d+$");

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

    private static async Task Main()
    {
        var arg = Environment.GetCommandLineArgs().LastOrDefault()?.Trim();

        Brain? brain;
        if (arg != null && _ipAddressRegex.IsMatch(arg))
        {
            brain = new(IPAddress.Parse(arg.Trim()));
        }
        else
        {
            Console.WriteLine("Discovering brain...");
            brain = await BrainDiscovery.DiscoverAsync();
        }
        if (brain is null)
        {
            Console.Error.WriteLine("Brain not found.");
            return;
        }
        Console.WriteLine($"Brain found! {brain.IPAddress}");

        try
        {
            List<IDeviceBuilder> devices = new();
            foreach (var type in Assembly.GetExecutingAssembly().GetExportedTypes())
            {
                if (type.IsAssignableTo(typeof(IDeviceProvider)) && !type.IsInterface && !type.IsAbstract)
                {
                    devices.Add(((IDeviceProvider)Activator.CreateInstance(type)!).ProvideDevice());
                }
            }

            await brain.StartServerAsync(devices);
            /*
             * bool switchValue = false;
    double sliderValue = 66d;
    UpdateNotifier notifier = _ => Task.CompletedTask;
    const string switchName = "switch";
    const string sliderName = "slider";
            */

            //IDeviceBuilder builder = Device.CreateDevice("Smart TV", DeviceType.TV)
            //    .SetDriverVersion(2)
            //    .SetManufacturer("Amir")
            //    .AddButton("INPUT HDMI1")
            //    .AddButtonGroup(ButtonGroup.NumberPad)
            //    .AddButtonGroup(ButtonGroup.ControlPad)
            //    .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            //    .AddSwitch(switchName, "Switch", GetSwitchValue, SetSwitchValue)
            //    .AddSlider(sliderName, "Slider", GetSliderValue, SetSliderValue)
            //    .AddButtonHandler(OnButtonPressed)
            //    .RegisterDeviceSubscriptionCallbacks(OnDeviceAdded, OnDeviceRemoved, InitializeDeviceList)
            //    .RegisterSubscriptionFunction((updateNotifier, _) => notifier = updateNotifier);
            //Console.WriteLine("Starting server...");
            //await brain.StartServerAsync(new[] { builder });
            //await Task.Delay(25000);
            ////await notifier(new("default", switchName, true));

            Console.WriteLine("Server started. Press any key to quit...");
            Console.ReadKey(true);
        }
        finally
        {
            Console.WriteLine("Server stopping...   ");
            await brain.StopServerAsync();
        }
        /*
        Task OnButtonPressed(string deviceId, string buttonName)
        {
            Console.WriteLine("Button: " + (KnownButton.TryGetKnownButton(buttonName) is KnownButtons button ? button : buttonName));
            return Task.CompletedTask;
        }

        Task<bool> GetSwitchValue(string deviceId)
        {
            Console.WriteLine("Get Switch:" + switchValue);
            return Task.FromResult(switchValue);
        }

        Task<double> GetSliderValue(string deviceId)
        {
            Console.WriteLine("Get Slider:" + sliderValue);
            return Task.FromResult(sliderValue);
        }

        Task SetSwitchValue(string deviceId, bool value)
        {
            if (switchValue == value)
            {
                return Task.CompletedTask;
            }
            Console.WriteLine("Set Switch: {0}", switchValue = value);
            return notifier(new(deviceId, switchName, value));
        }

        Task SetSliderValue(string deviceId, double value)
        {
            if (sliderValue == value)
            {
                return Task.CompletedTask;
            }
            Console.WriteLine("Set Switch: {0}", sliderValue = value);
            return notifier(new(deviceId, sliderName, value));
        }

        Task OnDeviceAdded(string deviceId)
        {
            Console.WriteLine("Device added: " + deviceId);
            return Task.CompletedTask;
        }

        Task OnDeviceRemoved(string deviceId)
        {
            Console.WriteLine("Device removed: " + deviceId);
            return Task.CompletedTask;
        }

        Task InitializeDeviceList(string[] deviceIds)
        {
            Console.WriteLine("Init deviceList: " + string.Join(',', deviceIds));
            return Task.CompletedTask;
        }*/
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