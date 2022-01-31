using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Neeo.Sdk.Json;

namespace Neeo.Sdk.Rest.Controllers;

internal static class ControllerMethods
{
    public static OkObjectResult Serialize<TValue>(this ControllerBase controller, [ActionResultObjectValue] TValue value)
    {
        OkObjectResult result = controller.Ok(value);
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
            try
            {
                if (context.ObjectType == typeof(Devices.Lists.IListBuilder))
                {
                    string text = JsonSerializer.Serialize(context.Object, context.ObjectType ?? typeof(object), JsonSerialization.Options);
                }

                await JsonSerializer.SerializeAsync(context.HttpContext.Response.Body, context.Object, context.ObjectType ?? typeof(object), JsonSerialization.Options, context.HttpContext.RequestAborted).ConfigureAwait(false);
                await context.HttpContext.Response.Body.FlushAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested) { }
        }
    }
}