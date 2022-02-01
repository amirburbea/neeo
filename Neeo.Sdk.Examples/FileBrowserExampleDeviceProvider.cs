using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Examples;

public class FileBrowserExampleDeviceProvider : IExampleDeviceProvider
{
    public IDeviceBuilder Provide()
    {
        return Device.Create("HOLYSHIT.cs", DeviceType.TV)
            .AddAdditionalSearchTokens("WTF")
            .AddButtons(KnownButtons.InputHdmi1)
            .SetSpecificName("HOLYSHIT.cs")
            .SetDriverVersion(2)
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .AddButtonHandler((_, __) => Task.CompletedTask)
            .AddDirectory("GENERIC_DIRECTORY", "GENERIC", identifier: "generic", role: null, populator: (x,y)=>Browse(x,y,false), actionHandler: OnDirectoryAction)
            .AddDirectory("ROOT_DIRECTORY", "ROOT", identifier: "root", role: DirectoryRole.Root, populator: (x, y) => Browse(x, y, false), actionHandler: OnDirectoryAction)
            .AddDirectory("QUEUE_DIRECTORY", "QUEUE", identifier: "queue", role: DirectoryRole.Queue, populator: (x, y) => Browse(x, y, true), actionHandler: OnDirectoryAction);
    }

    private Task Browse(string deviceId, IListBuilder builder, bool isQueue)
    {
        int offset = builder.Parameters.Offset ?? 0;
        int limit = builder.Parameters.Limit;
        if (string.IsNullOrEmpty(builder.Parameters.BrowseIdentifier))
        {
            builder.SetTitle("Drives").AddHeader(new("Drives"));
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                builder.AddEntry(new(title: drive.Name.TrimEnd('\\'), label: drive.Name, browseIdentifier: drive.Name.Replace('\\', '/').TrimEnd('/')));
            }
        }
        else
        {
            string root = builder.Parameters.BrowseIdentifier + '/';
            builder.SetTitle(root).AddHeader(new("Browse Folders"));
            if (offset == 0)
            {
                builder.AddTileRow(
                    new ListTile("https://neeo-sdk.neeo.io/puppy.jpg", "puppy"),
                    new ListTile("https://neeo-sdk.neeo.io/kitten.jpg", "kitten")
                ).AddInfoItem(new("Click me!", "These pics are cute, right?", "Definitely!", "No!", "INFO-OK"));

                builder.AddEntry(new("Reload the list!", actionIdentifier: "SOMETHING_RELOAD", uiAction: ListUIAction.Reload));
                builder.AddEntry(new("Go back to the root!", actionIdentifier: "SOMETHING_ROOT", uiAction: ListUIAction.GoToRoot));
                builder.AddEntry(new("Go back one step!", actionIdentifier: "SOMETHING_GOBACK", uiAction: ListUIAction.GoBack));
                builder.AddEntry(new("Close the list!", actionIdentifier: "SOMETHING_CLOSE", uiAction: ListUIAction.Close));

                builder.AddButtonRow(
                    new ListButton("Not inverted", actionIdentifier: "ABC"),
                    new ListButton("Inverted", actionIdentifier: "GOB", inverse: true, uiAction: ListUIAction.GoBack)
                );
            }
            try
            {
                var all = GetEntries(root,isQueue).ToList();
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

    private IEnumerable<ListEntry> GetEntries(string directory, bool isQueue)
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(directory).Select(p => p.Replace('\\', '/').TrimEnd('/')))
        {
            string title = Path.GetFileName(path);
            bool isDirectory = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            yield return new(
                title, 
                label: path,
                browseIdentifier: isDirectory ? path : null,
                actionIdentifier: isDirectory ? null : path, 
                isQueueable: isQueue || isDirectory ? null : true,
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