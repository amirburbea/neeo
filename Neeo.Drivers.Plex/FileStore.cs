using System;
using System.IO;
using System.Text.Json;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public interface IFileStore
{
    bool HasFile(string name);

    void DeleteFile(string name);

    TData Read<TData>(string name)
        where TData : notnull;

    string ReadText(string name);

    void Write<TData>(string name, TData data)
        where TData : notnull;

    void WriteText(string name, string text);
}

internal sealed class FileStore : IFileStore
{
    private readonly string _directoryPath = FileStore.GetDirectoryPath();

    public bool HasFile(string name)
    {
        return File.Exists(Path.Combine(this._directoryPath, name));
    }

    public void DeleteFile(string name)
    {
        File.Delete(Path.Combine(this._directoryPath, name));
    }

    public TData Read<TData>(string name) where TData : notnull
    {
        using Stream stream = File.OpenRead(Path.Combine(this._directoryPath, name));
        return JsonSerializer.Deserialize<TData>(stream, JsonSerialization.Options)!;
    }

    public string ReadText(string name)
    {
        return File.ReadAllText(Path.Combine(this._directoryPath, name));
    }

    public void Write<TData>(string name, TData data) where TData : notnull
    {
        using Stream stream = File.OpenWrite(Path.Combine(this._directoryPath, name));
        JsonSerializer.Serialize(stream, data, JsonSerialization.Options);
    }

    public void WriteText(string name, string text)
    {
        File.WriteAllText(Path.Combine(this._directoryPath, name), text);
    }

    private static string GetDirectoryPath()
    {
        string directoryPath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            typeof(FileStore).Assembly.GetName().Name!.ToLower()
        );
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }
}
