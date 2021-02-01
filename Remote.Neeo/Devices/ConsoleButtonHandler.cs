using System;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public sealed class ConsoleButtonHandler : IButtonHandler
    {
        public Task HandleButtonAsync(string button, string deviceId)
        {
            Console.WriteLine($"Button Pressed: {button} (Device: {deviceId})");
            return Task.CompletedTask;
        }
    }
}
