using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;
using Neeo.Sdk.Json;

namespace Remote.HodgePodge;

public class FileBrowserDeviceProvider : IDeviceProvider
{
    public IDeviceBuilder ProvideDevice()
    {
        return Device.Create("Directory Browser", DeviceType.TV)
            .AddAdditionalSearchTokens("WTF")
            .AddButtons(KnownButtons.InputHdmi1)
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .AddButtonHandler((_, __) => Task.CompletedTask)
            .AddDirectory("Files", "PC Files", identifier: default, role: default, populator: Browse, actionHandler: OnDirectoryAction);
    }

    private Task Browse(string deviceId, IListBuilder builder)
    {
        int offset = builder.Parameters.Offset??0;
        int limit = builder.Parameters.Limit;
        if (string.IsNullOrEmpty(builder.Parameters.BrowseIdentifier))
        {
            builder.SetTitle("Drives").AddHeader(new("Drives"));
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                builder.AddEntry(new(drive.Name.TrimEnd('\\'), drive.Name.Replace('\\', '/')));
            }
        }
        else
        {
            string root = builder.Parameters.BrowseIdentifier;
            builder.SetTitle(root);
            if (offset == 0)
            {
                builder.AddEntry(new("Close", null, "CLOSE", null, null, ListUIAction.Close));
                builder.AddEntry(new("Go Back", null, "GOBACK", null, null, ListUIAction.GoBack));

                //builder
                //    .AddEntry(new("CLOSE", null, null, null, null, uiAction: root.Length == 3 ? ListUIAction.Close : ListUIAction.GoBack));
                Console.WriteLine(JsonSerializer.Serialize(builder, JsonSerialization.Options));
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

    private IEnumerable<ListEntry> GetEntries(string directory)
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(directory).Select(p => p.Replace('\\', '/')))
        {
            string title = Path.GetFileName(path);
            bool isDirectory = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            yield return new(title, isDirectory ? path : null, isDirectory ? null : path, thumbnailUri: isDirectory ? "https://neeo-sdk.neeo.io/folder.jpg" : "https://neeo-sdk.neeo.io/file.jpg");
        }
    }


    private Task OnDirectoryAction(string deviceId, string actionIdentifier)
    {
        Console.WriteLine("DIRECTORY ACTION {0}:\"{1}\"", deviceId, actionIdentifier);
        return Task.CompletedTask;
    }
}