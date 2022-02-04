using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Examples.Devices;

public class FileBrowserExampleDevice : IDeviceProvider
{
    public IDeviceBuilder ProvideDevice()
    {
        const string deviceName = "File Browser Example";
        return Device.Create(deviceName, DeviceType.MediaPlayer)
            .SetSpecificName(deviceName)
            .SetIcon(DeviceIconOverride.NeeoBrain)
            .AddAdditionalSearchTokens("explorer")
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .AddDirectory("DIRECTORY", "Directory Browser", DirectoryRole.Root, populator: Browse, actionHandler: OnDirectoryAction);
    }

    private static Task Browse(string deviceId, IListBuilder builder)
    {
        int offset = builder.BrowseParameters.Offset ?? 0;
        int limit = builder.BrowseParameters.Limit;
        if (string.IsNullOrEmpty(builder.BrowseParameters.BrowseIdentifier))
        {
            const string title = "Drives";
            builder.SetTitle(title).AddHeader(title);
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string text = drive.Name.Replace('\\', '/');
                builder.AddEntry(new(title: text, label: text, browseIdentifier: drive.Name));
            }
        }
        else
        {
            try
            {
                string root = builder.BrowseParameters.BrowseIdentifier;
                ListEntry[] array = GetEntries(root).ToArray();
                string title = $"{root.Replace('\\', '/')} ({array.Length})";
                builder.SetTitle(title);
                if (offset == 0)
                {
                    builder
                        .AddHeader(title)
                        .AddTileRow(new("https://neeo-sdk.neeo.io/puppy.jpg", "puppy"), new("https://neeo-sdk.neeo.io/kitten.jpg", "kitten"))
                        .AddInfoItem(new("Click me!", "These pics are cute, right?", "Definitely!", "No!", "INFO-OK"))
                        .AddButtonRow(new("Reload", "RELOAD", inverse: false, uiAction: ListUIAction.Reload), new("BACK", "BACKONE", inverse: true, uiAction: ListUIAction.GoBack), new("ROOT", "BACKTOROOT", inverse: true, uiAction: ListUIAction.GoToRoot));
                }
                foreach (ListEntry entry in array[offset..Math.Min(offset + limit, array.Length)])
                {
                    builder.AddEntry(entry);
                }
                builder.SetTotalMatchingItems(array.Length);
            }
            catch (Exception e)
            {
                builder.AddInfoItem(new("Error Occurred", e.Message, "Close", "I Don't Care"));
            }
        }
        return Task.CompletedTask;
    }

    private static IEnumerable<ListEntry> GetEntries(string directory)
    {
        foreach (string fullPath in Directory.EnumerateFileSystemEntries(directory))
        {
            string fileName = Path.GetFileName(fullPath);
            bool isDirectory = (File.GetAttributes(fullPath) & FileAttributes.Directory) == FileAttributes.Directory;
            yield return new(
                title: fileName.Replace('\\', '/'),
                label: fullPath.Replace('\\', '/'),
                browseIdentifier: isDirectory ? fullPath : null,
                actionIdentifier: isDirectory ? null : fullPath,
                isQueueable: isDirectory ? null : true,
                thumbnailUri: isDirectory ? "https://neeo-sdk.neeo.io/folder.jpg" : "https://neeo-sdk.neeo.io/file.jpg"
            );
        }
    }

    private Task OnDirectoryAction(string deviceId, string actionIdentifier)
    {
        Console.WriteLine("DIRECTORY ACTION {0}:\"{1}\"", deviceId, actionIdentifier);
        return Task.CompletedTask;
    }
}