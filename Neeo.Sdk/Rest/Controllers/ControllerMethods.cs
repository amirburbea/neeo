using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Neeo.Sdk.Json;

namespace Neeo.Sdk.Rest.Controllers;

internal static class ControllerMethods
{
    public static OkObjectResult Ok<TValue>(this ControllerBase controller, [ActionResultObjectValue] TValue value)
    {
        OkObjectResult result = controller.Ok(value); // Call the non-generic version of Ok first.
        result.DeclaredType = typeof(TValue);
        result.Formatters.Add(JsonOutputFormatter.Instance);
        return result;
    }

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
                await JsonSerializer.SerializeAsync(httpContext.Response.Body, context.Object, context.ObjectType ?? typeof(object), JsonSerialization.Options, httpContext.RequestAborted).ConfigureAwait(false);
                await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested) { }
        }
    }
}