using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neeo.Drivers.WebOS;

public enum App
{
    Amazon,

    [App("com.webos.app.browser")]
    Browser,

    [App("com.disney.disneyplus-prod")]
    Disney,

    [App("com.hbo.hbomax")]
    HboMax,

    Netflix,

    [App("com.palm.app.settings")]
    Settings,

    [App("youtube.leanback.v4")]
    YouTube,
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class AppAttribute : Attribute
{
    public AppAttribute(string name) => this.Name = name;

    public string Name { get; }
}

public static class AppName
{
    private static readonly Dictionary<App, string> _names = new(
        from field in typeof(App).GetFields(BindingFlags.Static | BindingFlags.Public)
        select KeyValuePair.Create(
            (App)field.GetValue(null)!,
            field.GetCustomAttribute<AppAttribute>()?.Name ?? field.Name.ToLowerInvariant()
        )
    );

    public static string Of(App app) => AppName._names[app];
}