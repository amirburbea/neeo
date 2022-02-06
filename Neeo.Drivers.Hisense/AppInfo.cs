using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Hisense;

public readonly struct AppInfo : IEquatable<AppInfo>
{
    [JsonConstructor]
    public AppInfo(string name, string url) => (this.Name, this.Url) = (name, url);

    public string Name { get; }

    public string Url { get; }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is AppInfo info && this.Equals(info);

    public bool Equals(AppInfo other) => this.Name == other.Name && this.Url == other.Url;

    public override int GetHashCode() => HashCode.Combine(this.Name, this.Url);

    public static bool operator ==(AppInfo left, AppInfo right) => left.Equals(right);

    public static bool operator !=(AppInfo left, AppInfo right) => !left.Equals(right);
}