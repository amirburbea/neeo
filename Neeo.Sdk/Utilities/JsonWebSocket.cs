using System;
using System.Buffers;
using System.IO.Pipelines;
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
    /// <param name="options">
    /// The <see cref="JsonSerializerOptions"/> to deserialize messages with. Defaults to
    /// <see cref="JsonSerializerOptions.Web"/> - this is a general-purpose utility consumed by driver
    /// assemblies with their own message types, so it must not assume any particular caller's JSON
    /// configuration (e.g. Neeo.Sdk's own source-generated context, which only knows about Neeo.Sdk's
    /// own wire types).
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task MessageLoop<TMessage>(
        WebSocket webSocket,
        Func<TMessage, CancellationToken, ValueTask> processMessage,
        Action? onDisconnected = null,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default
    ) where TMessage : notnull
    {
        if (cancellationToken.IsCancellationRequested || webSocket.State is not WebSocketState.Open)
        {
            return;
        }
        Pipe pipe = new();
        // If reading stops for any reason (graceful completion or a fault from processMessage), cancel the
        // fill loop's in-flight/next ReceiveAsync promptly rather than leaving it blocked until the next
        // WebSocket message happens to arrive naturally.
        using CancellationTokenSource fillCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            Task fillTask = JsonWebSocket.FillPipeAsync(webSocket, pipe.Writer, fillCancellation.Token);
            Task readTask = JsonWebSocket.ReadPipeUntilCompleteAsync(pipe.Reader, processMessage, options, fillCancellation, cancellationToken);
            await Task.WhenAll(fillTask, readTask).ConfigureAwait(false);
            // Both sides completed without a fault - that only happens via a graceful close (or the
            // WebSocket's own state changing away from Open), which is a disconnection the caller needs
            // to know about, same as the WebSocketException case below.
            onDisconnected?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested, exit cleanly.
            return;
        }
        catch (WebSocketException)
        {
            // Anticipated network-level disconnect - notify and swallow.
            onDisconnected?.Invoke();
        }
        catch (Exception)
        {
            // Something unexpected (e.g. a bug in processMessage) killed the read loop. The socket's own
            // state is unaffected by this, so without notifying here, the caller would have no way of
            // knowing messages have stopped being read - it would look "connected" forever. Notify, then
            // rethrow so the failure is still visible/loggable to the caller rather than silently eaten.
            onDisconnected?.Invoke();
            throw;
        }
    }

    /// <summary>
    /// Reads raw bytes off the <paramref name="webSocket"/> and feeds them into <paramref name="writer"/>,
    /// with no awareness of WebSocket message boundaries or JSON framing - that's entirely the read
    /// side's job (see <see cref="TryReadMessage{TMessage}"/>). Completes the writer (with the triggering
    /// exception, if any) once the socket closes or an error occurs, which is what lets the reader side
    /// know there's no more data coming.
    /// </summary>
    private static async Task FillPipeAsync(WebSocket webSocket, PipeWriter writer, CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                Memory<byte> memory = writer.GetMemory(8192);
                ValueWebSocketReceiveResult result = await webSocket.ReceiveAsync(memory, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                writer.Advance(result.Count);
                FlushResult flushResult = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled)
                {
                    // The reader side is done (gracefully or otherwise) - stop producing.
                    break;
                }
            }
        }
        catch (Exception e)
        {
            error = e;
        }
        finally
        {
            await writer.CompleteAsync(error).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads complete JSON values out of <paramref name="reader"/>'s buffered bytes as they become
    /// available, deserializing and dispatching each to <paramref name="processMessage"/> in turn.
    /// </summary>
    private static async Task ReadPipeUntilCompleteAsync<TMessage>(
        PipeReader reader,
        Func<TMessage, CancellationToken, ValueTask> processMessage,
        JsonSerializerOptions? options,
        CancellationTokenSource fillCancellation,
        CancellationToken cancellationToken
    ) where TMessage : notnull
    {
        try
        {
            ReadResult result;
            do
            {
                result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;
                while (JsonWebSocket.TryReadMessage(ref buffer, result.IsCompleted, options, out TMessage? message))
                {
                    if (message is not null)
                    {
                        await processMessage(message, cancellationToken).ConfigureAwait(false);
                    }
                }
                reader.AdvanceTo(buffer.Start, buffer.End);
            }
            while (!result.IsCompleted);
        }
        finally
        {
            await reader.CompleteAsync().ConfigureAwait(false);
            // See the comment on fillCancellation above - ensures FillPipeAsync's ReceiveAsync unblocks
            // promptly if this method is exiting due to a fault rather than a graceful pipe completion.
            fillCancellation.Cancel();
        }
    }

    /// <summary>
    /// Attempts to parse exactly one complete top-level JSON value out of the front of
    /// <paramref name="buffer"/>, slicing it off on success. Returns <see langword="false"/> (without
    /// slicing anything) when the buffer doesn't yet contain a complete value - the caller should read
    /// more data and try again.
    /// </summary>
    private static bool TryReadMessage<TMessage>(ref ReadOnlySequence<byte> buffer, bool isFinalBlock, JsonSerializerOptions? options, out TMessage? message)
        where TMessage : notnull
    {
        if (buffer.IsEmpty)
        {
            message = default;
            return false;
        }
        Utf8JsonReader jsonReader = new(buffer, isFinalBlock, default);
        if (!JsonDocument.TryParseValue(ref jsonReader, out JsonDocument? document))
        {
            message = default;
            return false;
        }
        using (document)
        {
            message = document.Deserialize<TMessage>(options ?? JsonSerializerOptions.Web);
        }
        buffer = buffer.Slice(jsonReader.BytesConsumed);
        return true;
    }
}
