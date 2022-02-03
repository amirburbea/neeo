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
            .SetManufacturer("NEEO")
            .SetIcon(DeviceIconOverride.NeeoBrain)
            .AddAdditionalSearchTokens("explorer")
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .AddDirectory("DIRECTORY", "Directory Browser", DirectoryRole.Root, populator: Browse, actionHandler: OnDirectoryAction);
    }

    private static Task Browse(string deviceId, IListBuilder builder)
    {
        int offset = builder.Parameters.Offset ?? 0;
        int limit = builder.Parameters.Limit;
        if (string.IsNullOrEmpty(builder.Parameters.BrowseIdentifier))
        {
            builder.SetTitle("Drives").AddHeader(new("Drives"));
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string text = drive.Name.Replace('\\', '/');
                builder.AddEntry(new(title: text, label: text, browseIdentifier: drive.Name));
            }
        }
        else
        {
            string root = builder.Parameters.BrowseIdentifier;
            builder.SetTitle(root).AddHeader(new(root.Replace('\\', '/')));
            if (offset == 0)
            {
                //builder.AddTileRow(
                //    new ListTile("https://neeo-sdk.neeo.io/puppy.jpg", "puppy"),
                //    new ListTile("https://neeo-sdk.neeo.io/kitten.jpg", "kitten")
                //).AddInfoItem(new("Click me!", "These pics are cute, right?", "Definitely!", "No!", "INFO-OK"));

                builder.AddButtonRow(
                    new ListButton("Reload", "RELOAD", inverse: false, uiAction: ListUIAction.Reload),
                    new ListButton("BACK", "BACKONE", inverse: true, uiAction: ListUIAction.GoBack),
                    new ListButton("ROOT", "BACKTOROOT", inverse: true, uiAction: ListUIAction.GoToRoot)
                );
            }
            try
            {
                var all = GetEntries(root).ToList();
                foreach (var entry in all.Skip(offset).Take(limit))
                {
                    builder.AddEntry(entry);
                }
                builder.SetTotalMatchingItems(all.Count);
            }
            catch (Exception e)
            {
                builder.AddInfoItem(new("Error Occurred", e.Message, "CLOSE", "DON'T CARE"));
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