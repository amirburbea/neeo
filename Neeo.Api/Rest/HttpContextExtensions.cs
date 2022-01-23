using Microsoft.AspNetCore.Http;

namespace Neeo.Api.Rest;

internal static class HttpContextExtensions
{
    public static T? GetItem<T>(this HttpContext context) => context.Items.TryGetValue(typeof(T).Name, out object? value) && value is T item ? item : default;

    public static bool HasItem<T>(this HttpContext context) => context.Items.ContainsKey(typeof(T).Name);

    public static void SetItem<T>(this HttpContext context, T item) => context.Items[typeof(T).Name] = item;
}