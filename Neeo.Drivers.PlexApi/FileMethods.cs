using System.IO;

namespace Neeo.Drivers.PlexApi;

internal static class FileMethods
{
    public static bool IsDirectoryWritable(string path)
    {
        if (!Directory.Exists(path))
        {
            return false;
        }
        try
        {
            using FileStream _ = File.Create(Path.Combine(path, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
