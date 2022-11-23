using System;
using System.Collections.Generic;

namespace Neeo.Drivers.WebOS;

public enum App
{
    Amazon,

    [App("com.apple.appletv")]
    AppleTV,

    [App("com.webos.app.browser")]
    Browser,

    [App("com.disney.disneyplus-prod")]
    Disney,

    [App("com.dolby.lgapp")]
    DolbyAccess,

    [App("com.hbo.hbomax")]
    HboMax,

    [App("com.webos.app.homeconnect")]
    HomeDashboard,

    Netflix,

    [App("com.palm.app.settings")]
    Settings,

    [App("spotify-beehive")]
    Spotify,

    [App("com.webos.app.discovery")]
    Store,

    [App("youtube.leanback.v4")]
    YouTube,
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class AppAttribute : Attribute, INameAttribute
{
    public AppAttribute(string name) => this.Name = name;

    public string Name { get; }
}

public static class AppName
{
    private static readonly IReadOnlyDictionary<App, string> _names = NameDictionary.Generate<App, AppAttribute>();

    public static string Of(App app) => AppName._names[app];
}