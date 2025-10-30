using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Neeo.Drivers.PlexApi;

public interface IPlexSettingsManager
{
    T? Deserialize<T>(string settingsFile) where T : notnull;

    bool HasFile(string settingsFile);

    byte[] ReadAllBytes(string settingsFile);

    string ReadAllText(string settingsFile) => Encoding.UTF8.GetString(this.ReadAllBytes(settingsFile));

    void Serialize<T>(string settingsFile, T value) where T : notnull;

    void WriteAllBytes(string settingsFile, ReadOnlySpan<byte> bytes);

    void WriteAllText(string settingsFile, string text) => this.WriteAllBytes(settingsFile, Encoding.UTF8.GetBytes(text));
}

internal sealed class PlexSettingsManager : IPlexSettingsManager
{
    private readonly string _settingsPath = PlexSettingsManager.DetermineSettingsPath();

    public T? Deserialize<T>(string settingsFile)
        where T : notnull
    {
        using FileStream stream = File.OpenRead(Path.Combine(this._settingsPath, settingsFile));
        return JsonSerializer.Deserialize<T>(stream, JsonSerializerOptions.Web);
    }

    public bool HasFile(string settingsFile)
    {
        return File.Exists(Path.Combine(this._settingsPath, settingsFile));
    }

    public void Serialize<T>(string settingsFile, T value)
        where T : notnull
    {
        using FileStream stream = File.Create(Path.Combine(this._settingsPath, settingsFile));
        JsonSerializer.Serialize(stream, value, JsonSerializerOptions.Web);
    }

    public byte[] ReadAllBytes(string settingsFile)
    {
        return File.ReadAllBytes(Path.Combine(this._settingsPath, settingsFile));
    }

    public void WriteAllBytes(string settingsFile, ReadOnlySpan<byte> bytes)
    {
        File.WriteAllBytes(Path.Combine(this._settingsPath, settingsFile), bytes);
    }

    private static string DetermineSettingsPath()
    {
        string fallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neeo");
        string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            "Production";
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .Build();
        string directory = config["settingsPath"] ?? fallbackDirectory;
        if (!IsWritable(directory))
        {
            // If we received a non-default path that is not writable, fall back to the default path if it is writable.
            if (directory == fallbackDirectory || !IsWritable(fallbackDirectory))
            {
                throw new UnauthorizedAccessException($"The configured settings path '{directory}' is not writable.");
            }
            directory = fallbackDirectory;
        }
        return Path.GetFullPath(directory);

        static bool IsWritable(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                using FileStream _ = File.Create(Path.Combine(directoryPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
