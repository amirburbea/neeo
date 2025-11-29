using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SocketIO.Core;
using SocketIO.Serializer.SystemTextJson;

namespace Neeo.Sdk.Spoke;

using Client = SocketIOClient.SocketIO;

internal class Program
{
    static async Task Main()
    {

        /*
        using CancellationTokenSource cts = new();
        (Channel<SocketIOMessage> channel, Task[] tasks) = CreateChannel(capacity: 20, workerCount: 4, cts.Token);
        using Client client = new("http://192.168.253.252:3000", new() { EIO = EngineIO.V3 }) { Serializer = new SystemTextJsonSerializer(JsonSerializerOptions.Web) };
        client.OnConnected += async (sender, e) =>
        {
            Console.WriteLine("Connected to server, emitting register");
            await client.EmitAsync("send", "ping").ConfigureAwait(false);
            Console.WriteLine("Emitted register message.");
        };
        client.OnDisconnected += (sender, e) =>
        {
            Console.WriteLine("Disconnected from server.");
        };
        client.OnAny(async (eventName, response) =>
        {
            try
            {
                await channel.Writer.WriteAsync(new(eventName, response.GetValue<JsonElement>()), cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {eventName}:{ex.Message}");
            }
        });
        await client.ConnectAsync().ConfigureAwait(false);
        Console.WriteLine("Press ENTER to END");
        Console.ReadLine();
        channel.Writer.Complete();
        await client.DisconnectAsync().ConfigureAwait(false);
        cts.Cancel();
        await Task.WhenAll(tasks).ConfigureAwait(false);
        Console.WriteLine("Done");
    }

    private static (Channel<SocketIOMessage>, Task[]) CreateChannel(int capacity, int workerCount, CancellationToken cancellationToken)
    {
        Channel<SocketIOMessage> channel = Channel.CreateBounded<SocketIOMessage>(options: new(capacity) { FullMode = BoundedChannelFullMode.Wait });
        Task[] tasks = new Task[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            tasks[i] = Task.Run(() => ProcessMessages(channel, cancellationToken), cancellationToken);
        }
        return (channel, tasks);

        static async Task ProcessMessages(Channel<SocketIOMessage> channel, CancellationToken cancellationToken)
        {
            await foreach ((string Event, JsonElement Data) in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                Console.WriteLine($"Processing in worker: {Event}:{Data}");
            }
        }
    }

    public readonly record struct SocketIOMessage(string Event, JsonElement Data);
        */
    }
}
