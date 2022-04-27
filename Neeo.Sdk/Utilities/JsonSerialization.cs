using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Contains the <see langword="static"/> JSON serialization options to use with the NEEO Brain.
/// </summary>
public static class JsonSerialization
{
    /// <summary>
    /// <see cref="JsonSerializerOptions"/> suitable for interactions with the NEEO <see cref="Brain"/>.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Converts the <see cref="JsonElement" /> representing a single JSON value into a <typeparamref name="TValue" />.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <param name="element">The JSON element to deserialize.</param>
    /// <returns>The <typeparamref name="TValue"/> representation of the JSON value.</returns>
    public static TValue Deserialize<TValue>(this JsonElement element) => element.Deserialize<TValue>(JsonSerialization.Options)!;

    internal static OkObjectResult Ok<TValue>([ActionResultObjectValue] TValue value) => new(value)
    {
        DeclaredType = typeof(TValue),
        Formatters = { JsonOutputFormatter<TValue>.Instance }
    };

    /// <summary>
    /// The standard SystemTextJsonOutputFormatter calls <see cref="object.GetType"/> on objects to be serialized.
    /// This instead ensures objects are serialized directly as the intended response type - useful when explicit properties
    /// are used or JSON property attributes are applied on an interface as opposed to the implemented type.
    /// </summary>
    private sealed class JsonOutputFormatter<TValue> : IOutputFormatter
    {
        public static readonly JsonOutputFormatter<TValue> Instance = new();

        private JsonOutputFormatter()
        {
        }

        public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            try
            {
                await JsonSerializer.SerializeAsync(context.HttpContext.Response.Body, (TValue)context.Object!, JsonSerialization.Options, context.HttpContext.RequestAborted).ConfigureAwait(false);
                await context.HttpContext.Response.Body.FlushAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested) { }
        }
    }
}