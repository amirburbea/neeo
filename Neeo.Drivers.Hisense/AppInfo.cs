using System;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Hisense;

public record struct AppInfo(String Name, String Url,[property:JsonPropertyName("isunInstalled")] bool IsUninstalled);