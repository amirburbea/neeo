using System;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Hisense;

public record class AppInfo(
    string Name, 
    string Url,
    [property:JsonPropertyName("isunInstalled")] bool IsUninstalled = false
);