using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Neeo.Drivers.Plex;

public interface IPlexSettingsManager
{
    bool HasFile(string settingsFile);

    byte[] ReadAllBytes(string settingsFile);

    string ReadAllText(string settingsFile) => Encoding.UTF8.GetString(this.ReadAllBytes(settingsFile));

    void WriteAllBytes(string settingsFile, ReadOnlySpan<byte> bytes);

    void WriteAllText(string settingsFile, string text) => this.WriteAllBytes(settingsFile, Encoding.UTF8.GetBytes(text));
}

internal sealed class PlexSettingsManager : IPlexSettingsManager
{
    private readonly string _settingsPath = PlexSettingsManager.DetermineSettingsPath();

    public bool HasFile(string settingsFile) => File.Exists(Path.Combine(this._settingsPath, settingsFile));

    public byte[] ReadAllBytes(string settingsFile) => File.ReadAllBytes(Path.Combine(this._settingsPath, settingsFile));

    public void WriteAllBytes(string settingsFile, ReadOnlySpan<byte> bytes) => File.WriteAllBytes(Path.Combine(this._settingsPath, settingsFile), bytes);

    private static string DetermineSettingsPath()
    {
        string fallbackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neeo");
        string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            "Production";
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();
        string directory = config["settingsPath"] ?? fallbackDirectory;
        if (!IsWritable(directory))
        {
            // If we received a non-default path that is not writable, fall back to the default path
            // if it is writable.
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
                using (File.Create(Path.Combine(directoryPath, Path.GetRandomFileName()), bufferSize: 1, FileOptions.DeleteOnClose))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
