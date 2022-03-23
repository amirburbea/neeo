using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest.Controllers;

internal static class JsonResult
{
    public static OkObjectResult Ok<TValue>([ActionResultObjectValue] TValue value) => new(value)
    {
        DeclaredType = typeof(TValue),
        Formatters = { JsonOutputFormatter.Instance }
    };

    /// <summary>
    /// The standard SystemTextJsonOutputFormatter calls <see cref="object.GetType"/> on objects to be serialized.
    /// This instead ensures objects are serialized directly as the intended response type - useful when explicit properties
    /// are used or JSON property attributes are applied on an interface as opposed to the implemented type.
    /// </summary>
    private sealed class JsonOutputFormatter : IOutputFormatter
    {
        public static readonly JsonOutputFormatter Instance = new();

        private JsonOutputFormatter()
        {
        }

        public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

        public async Task WriteAsync(OutputFormatterWriteContext context)
        {
            HttpContext httpContext = context.HttpContext;
            try
            {
                await JsonSerializer.SerializeAsync(
                    httpContext.Response.Body,
                    context.Object,
                    context.ObjectType ?? typeof(object),
                    JsonSerialization.Options,
                    httpContext.RequestAborted
                ).ConfigureAwait(false);
                await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested) { }
        }
    }
}