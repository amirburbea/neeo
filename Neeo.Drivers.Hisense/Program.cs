using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;

namespace Neeo.Drivers.Hisense;

internal static class Program
{
    //private static async Task Main()
    //{
    //    HisenseTV.ClientIdSuffix = Guid.NewGuid().ToString();
    //    if (await HisenseTV.DiscoverOneAsync() is { } tv && tv.IsConnected)
    //    {
    //        var connection = tv.GetConnection()!;
    //        var state = await connection.GetStateAsync();
    //        while (state.Type == StateType.AuthenticationRequired)
    //        {
    //            Console.Write("Enter Code:");
    //            string code = Console.ReadLine()!;
    //            state = await connection.AuthenticateAsync(code);
    //            if (state.Type == StateType.AuthenticationRequired)
    //            {
    //                Console.WriteLine("You fucked up.");
    //            }
    //        }
    //        Console.WriteLine(state);
    //        AppInfo[] apps = await connection.GetAppsAsync();
    //        Console.WriteLine("Apps:[" + string.Join('|', apps) + "]");
    //        int volume = await connection.GetVolumeAsync();
    //        Console.WriteLine("Volume:" + volume);
    //        Console.WriteLine("Press Enter to quit");
    //        connection.VolumeChanged += Connection_VolumeChanged;


    //        void Connection_VolumeChanged(object? sender, VolumeChangedEventArgs e)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        Console.ReadLine();
    //    }
    //}

    public static async Task Main()
    {
        //TaskCompletionSource<HisenseClient?> taskCompletionSource = new();
        //ThreadPool.QueueUserWorkItem(_ => DiscoverAsync());
        //if (await taskCompletionSource.Task.ConfigureAwait(false) is { } client)
        //{
        //    Console.WriteLine(client);
        //}

        //async void DiscoverAsync()
        //{
        //    using CancellationTokenSource cts = new();
        //    await Task.WhenAll(
        //        NetworkDevices.GetNetworkDevices().Select(async pair =>
        //        {
        //            (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
        //            if (await Program.ConnectAsync(ipAddress, macAddress, cts.Token).ConfigureAwait(false) is not { } client)
        //            {
        //                return;
        //            }
        //            taskCompletionSource.TrySetResult(client);
        //            cts.Cancel();
        //        })
        //    ).ConfigureAwait(false);
        //    taskCompletionSource.TrySetResult(default);
        //}
       

        using var x = await ConnectAsync(IPAddress.Parse("192.168.253.147"), PhysicalAddress.Parse("7c-b3-7b-ad-2b-9a"), default);
        Console.WriteLine("X");
    }

    private static async Task<HisenseClient?> ConnectAsync(IPAddress ipAddress, PhysicalAddress macAddress, CancellationToken cancellationToken)
    {
        if (!await Program.TryPingAsync(ipAddress, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }
        HisenseClient client = new(ipAddress, macAddress);
        try
        {
            if (await client.ConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                await client.RequestLogin().ConfigureAwait(false);
                Console.Write("ENTER CODE:");
                if (Console.ReadLine() is { Length: <= 4 } str && int.TryParse(str, out _))
                {
                    await client.TryLogin(str);

                    while (true)
                    {
                        Console.Write("ENTER subject:");
                        string? subject = Console.ReadLine();

                        Console.Write("ENTER action:");
                        string? action = Console.ReadLine();
                        if (subject is not { Length: >= 1 } || action is not { Length: >= 1 })
                        {
                            continue;
                        }
                        object? payload = null;
                        if (action == "launchapp")
                        {
                            string appName = "Netflix";
                            payload = new { Name = appName, UrlType = 37, StoreType = 0, Url = "com.netflix.ninja" };
                        }
                        await client.SendMessageAsync(subject, action,payload);
                    }
                    return client;
                }
            }
        }
        catch (MqttCommunicationException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        client.Dispose();
        return null;
    }

    private static async Task<bool> TryPingAsync(IPAddress address, CancellationToken cancellationToken)
    {
        using Ping ping = new();
        TaskCompletionSource<bool> taskSource = new();
        await using (cancellationToken.Register(OnCancellationRequested).ConfigureAwait(false))
        {
            ping.PingCompleted += OnPingCompleted;
            ping.SendAsync(address, 15, taskSource);
            bool success = await taskSource.Task.ConfigureAwait(false);
            ping.PingCompleted -= OnPingCompleted;
            return success;

            static void OnPingCompleted(object? _, PingCompletedEventArgs e) => ((TaskCompletionSource<bool>)e.UserState!).TrySetResult(e.Reply?.Status == IPStatus.Success);
        }

        void OnCancellationRequested()
        {
            ping.SendAsyncCancel();
            taskSource.TrySetCanceled(cancellationToken);
        }
    }
}