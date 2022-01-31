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

public class FileBrowserDeviceProvider : IExampleDeviceProvider
{
    public IDeviceBuilder ProvideDevice()
    {
        return Device.Create("HOLYSHIT.cs", DeviceType.TV)
            .AddAdditionalSearchTokens("WTF")
            .AddButtons(KnownButtons.InputHdmi1)
            .SetSpecificName("HOLYSHIT.cs")
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .AddButtonHandler((_, __) => Task.CompletedTask)
            .AddDirectory("BROWSE_DIRECTORY", "HOLYSHIT.cs", identifier: "Drive", role: DirectoryRole.Root, populator: Browse, actionHandler: OnDirectoryAction);
    }

    private Task Browse(string deviceId, IListBuilder builder)
    {
        string? browseIdentifier = builder.Parameters.BrowseIdentifier;





        int offset = builder.Parameters.Offset ?? 0;
        int limit = builder.Parameters.Limit;
        if (string.IsNullOrEmpty(builder.Parameters.BrowseIdentifier ))
        {
            builder.SetTitle("Drives").AddHeader(new("Drives"));
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                builder.AddEntry(new(drive.Name.TrimEnd('\\'), drive.Name.Replace('\\', '/').TrimEnd('/')));
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





                builder.AddEntry(new("Reload the list!", null, "SOMETHING_RELOAD", null, null, ListUIAction.Reload));
                builder.AddEntry(new("Go back to the root!", null, "SOMETHING_ROOT", null, null, ListUIAction.GoToRoot));
                builder.AddEntry(new("Go back one step!", null, "SOMETHING_GOBACK", null, null, ListUIAction.GoBack));
                builder.AddEntry(new("Close the list!", null, "SOMETHING_CLOSE", null, null, ListUIAction.Close));

                builder.AddButtonRow(new(new ListButton[] {
                    new ListButton("Not inverted","ABC"),
                    new ListButton("Inverted","GOB",inverse:true, uiAction: ListUIAction.GoBack)

                })); 
               // Console.WriteLine(JsonSerializer.Serialize(builder, JsonSerialization.Options));
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
        foreach (string path in Directory.EnumerateFileSystemEntries(directory).Select(p => p.Replace('\\', '/').TrimEnd('/')))
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