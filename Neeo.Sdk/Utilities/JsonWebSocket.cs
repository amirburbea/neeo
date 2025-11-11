using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Contains utilities for reading JSON data from a web socket.
/// </summary>
public static class JsonWebSocket
{
    /// <summary>
    /// Listens to messages received from the <paramref name="webSocket"/>,
    /// deserializing them as JSON to <typeparamref name="TMessage"/>,
    /// and processes them via <paramref name="processMessage"/>.
    /// 
    /// In the event of an unexpected disconnection, invokes <paramref name="onDisconnected"/>.
    /// </summary>
    /// <typeparam name="TMessage">Type of the JSON messages received.</typeparam>
    /// <param name="webSocket">The websocket on which to listen for messages.</param>
    /// <param name="processMessage">Callback to asynchronously process received messages.</param>
    /// <param name="onDisconnected">Optional callback invoked upon disconnection.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task MessageLoop<TMessage>(
        WebSocket webSocket,
        Func<TMessage, CancellationToken, Task> processMessage,
        Action? onDisconnected = null,
        CancellationToken cancellationToken = default
    ) where TMessage : notnull
    {
        byte[] previous = [];
        try
        {
            if (cancellationToken.IsCancellationRequested || webSocket.State is not WebSocketState.Open)
            {
                return;
            }
            int previousLength = 0;
            using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(32768);
            while (webSocket.State == WebSocketState.Open && await webSocket.ReceiveAsync(owner.Memory, cancellationToken).ConfigureAwait(false) is { MessageType: not WebSocketMessageType.Close } result)
            {
                if (previous.Length == 0 && result.EndOfMessage)
                {
                    // Complete message was received.
                    await ProcessAsync(owner.Memory.Span[..result.Count]).ConfigureAwait(false);
                    continue;
                }
                // Combine previous fragment with incoming one.
                int nextLength = previousLength + result.Count;
                byte[] next = ArrayPool<byte>.Shared.Rent(nextLength);
                if (previous.Length != 0)
                {
                    previous.AsSpan(0, previousLength).CopyTo(next);
                    ArrayPool<byte>.Shared.Return(previous);
                }
                owner.Memory[0..result.Count].CopyTo(next.AsMemory(previousLength));
                if (!result.EndOfMessage)
                {
                    (previous, previousLength) = (next, nextLength);
                    continue;
                }
                (previous, previousLength) = ([], 0);
                try
                {
                    await ProcessAsync(next.AsSpan(0, nextLength)).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(next);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested, exit cleanly.
            return;
        }
        catch (WebSocketException)
        {
            onDisconnected?.Invoke();
        }
        finally
        {
            if (previous.Length != 0)
            {
                ArrayPool<byte>.Shared.Return(previous);
            }
        }

        Task ProcessAsync(ReadOnlySpan<byte> span) => JsonSerializer.Deserialize<TMessage>(span, JsonSerializerOptions.Web) is { } message
            ? processMessage(message, cancellationToken)
            : Task.CompletedTask;
    }
}
