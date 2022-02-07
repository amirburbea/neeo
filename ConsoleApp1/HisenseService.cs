using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.Hisense;

namespace ConsoleApp1;

internal class HisenseService : IHostedService
{
    private readonly ILogger<HisenseService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public HisenseService(ILogger<HisenseService> logger, IHostApplicationLifetime lifetime)
    {
        this._logger = logger;
        this._lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Starting");
        if (await HisenseTV.DiscoverOneAsync(nameof(HisenseService), cancellationToken).ConfigureAwait(false) is not { } tv)
        {
            throw new("Failed to discover TV (WTF?)");
        }
        ThreadPool.QueueUserWorkItem(state => this.Run(state, this._lifetime.ApplicationStopping), tv);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private record struct TVContainer(HisenseTV TV);

    private async void Run(object? arg, CancellationToken cancellationToken)
    {
        using HisenseTV tv = (HisenseTV)arg!;
        this._logger.LogInformation("Requesting state");
        if (await tv.GetStateAsync(cancellationToken).ConfigureAwait(false) is not { } state)
        {
            this._logger.LogError("Failed to get state.");
            return;
        }
        while (state.Type == StateType.AuthenticationRequired)
        {
            Console.Write("enter code: ");
            string code = Console.ReadLine()!; 
            this._logger.LogInformation("Requesting state");
            state = await tv.AuthenticateAsync(code).ConfigureAwait(false);
        }
        Console.WriteLine("TV state: {0}", state);
        tv.StateChanged += TV_StateChanged;
        tv.Disconnected += TV_Disconnected;
        tv.Sleep += TV_Sleep;
        tv.VolumeChanged += TV_VolumeChanged;
        tv.Connected += TV_Connected;
        while (true)
        {
            Console.WriteLine(new string(' ', 32));
            Console.Write("Enter (case-insensitive) command (or HELP for list):");
            switch (Console.ReadLine()?.ToUpper())
            {
                case "HELP":
                    Console.WriteLine("HELP - This list");
                    Console.WriteLine("NETFLIX - Netflix");
                    Console.WriteLine("GOOGLEPLAY - Launch google play");
                    Console.WriteLine("HOME - Go home");
                    Console.WriteLine("VOLUME - Get the volume and explicitly send -2");
                    Console.WriteLine("VOLUMEDOWN - Send the volume down remote command");
                    Console.WriteLine("VOLUMEUP - Send the volume up remote command");
                    Console.WriteLine("APPS - Get the list of apps");
                    Console.WriteLine("SOURCES - Get the list of sources");
                    Console.WriteLine("SOURCE_HDMI1 - Change to HDMI1");
                    break;
                case "NETFLIX":
                    await tv.LaunchAppAsync("Netflix", cancellationToken).ConfigureAwait(false);
                    break;
                case "GOOGLEPLAY":
                    await tv.LaunchAppAsync("Play Store", cancellationToken).ConfigureAwait(false);
                    break;
                case "HOME":
                    await tv.SendKeyAsync(RemoteKey.Home, cancellationToken).ConfigureAwait(false);
                    break;
                case "VOLUMEUP":
                    await tv.SendKeyAsync(RemoteKey.VolumeUp, cancellationToken).ConfigureAwait(false);
                    break;
                case "VOLUMEDOWN":
                    await tv.SendKeyAsync(RemoteKey.VolumeDown, cancellationToken).ConfigureAwait(false);
                    break;
                case "VOLUME":
                    int volume = await tv.GetVolumeAsync(cancellationToken).ConfigureAwait(false);
                    Console.WriteLine("Current volume: " + volume);
                    int nextVolume = Math.Max(volume - 2, 0);
                    await tv.ChangeVolumeAsync(nextVolume, cancellationToken).ConfigureAwait(false);
                    break;
                case "APPS":
                    AppInfo[] apps = await tv.GetAppsAsync(cancellationToken).ConfigureAwait(false);
                    PrintArray(apps);
                    break;
                case "SOURCES":
                    SourceInfo[] sources = await tv.GetSourcesAsync(cancellationToken).ConfigureAwait(false);
                    PrintArray(sources);
                    break;
                case "SOURCE_HDMI1":
                    await tv.ChangeSourceAsync("HDMI 1", cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        static void PrintArray<T>(T[] array) => Console.WriteLine($"[{string.Join('|', array)}]");

        void TV_StateChanged(object? sender, StateChangedEventArgs e) => Console.WriteLine($"State changed: {e.State}");

        void TV_Connected(object? sender, EventArgs e) => Console.WriteLine("TV reconnected");

        void TV_Disconnected(object? sender, EventArgs e) => Console.WriteLine("TV disconnected");

        void TV_Sleep(object? sender, EventArgs e) => Console.WriteLine("TV sleep");

        void TV_VolumeChanged(object? sender, VolumeChangedEventArgs e) => Console.WriteLine($"TV VolumeChanged: {e.Volume}");
    }

    //private async void Run(object? state)
    //{
    //    using HisenseTV tv = (HisenseTV)state!;
    //    Canc
    //    if(await tv.GetStateAsync(

    //}
}