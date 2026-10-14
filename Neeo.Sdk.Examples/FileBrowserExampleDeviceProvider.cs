using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Sdk.Examples;

public class FileBrowserExampleDeviceProvider : IDeviceProvider
{
    public FileBrowserExampleDeviceProvider()
    {
        const string deviceName = "SDK File Browser Example";
        this.DeviceBuilder = Device.Create(deviceName, DeviceType.Accessory)
            .SetSpecificName(deviceName)
            .SetIcon(DeviceIconOverride.NeeoBrain)
            .AddAdditionalSearchTokens("explorer")
            .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
            .EnableDeviceRoute(prefix => this._uriPrefix = prefix, EmbeddedImages.HandleRequestAsync)
            .AddDirectory("DIRECTORY", "Directory Browser", DirectoryRole.Root, browser: this.Browse, actionHandler: this.OnDirectoryAction);
    }

    public IDeviceBuilder DeviceBuilder { get; }

    private string _uriPrefix = string.Empty;

    private Task Browse(string deviceId, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        int offset = builder.Parameters.Offset;
        int limit = builder.Parameters.Limit;
        if (string.IsNullOrEmpty(builder.Parameters.BrowseIdentifier))
        {
            const string title = "Drives";
            builder.SetTitle(title).AddHeader(title);
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string text = drive.Name.Replace('\\', '/');
                builder.AddEntry(new(Title: text, Label: text, BrowseIdentifier: drive.Name));
            }
        }
        else
        {
            try
            {
                string root = builder.Parameters.BrowseIdentifier;
                DirectoryEntry[] array = [.. this.GetEntries(root)];
                string title = $"{root.Replace('\\', '/')} ({array.Length})";
                builder.SetTitle(title);
                if (offset == 0)
                {
                    builder
                        .AddHeader(title)
                        .AddTileRow([new($"{this._uriPrefix}puppy.jpg", "puppy")])
                        .AddInfoItem(new("Click me!", "These pics are cute, right?", AffirmativeButtonText: "Definitely!", NegativeButtonText: "No!", ActionIdentifier: "INFO-OK"))
                        .AddButtonRow(
                            new("Reload", ActionIdentifier: "RELOAD", Inverse: false, UIAction: DirectoryUIAction.Reload),
                            new("BACK", ActionIdentifier: "BACKONE", Inverse: true, UIAction: DirectoryUIAction.GoBack),
                            new("ROOT", ActionIdentifier: "BACKTOROOT", Inverse: true, UIAction: DirectoryUIAction.GoToRoot)
                        );
                }
                foreach (DirectoryEntry entry in array[offset..Math.Min(offset + limit, array.Length)])
                {
                    builder.AddEntry(entry);
                }
                builder.SetTotalMatchingItems(array.Length);
            }
            catch (Exception e)
            {
                builder.AddInfoItem(new("Error Occurred", e.Message, AffirmativeButtonText: "Close", NegativeButtonText: "I Don't Care"));
            }
        }
        return Task.CompletedTask;
    }

    private IEnumerable<DirectoryEntry> GetEntries(string directory)
    {
        foreach (string fullPath in Directory.EnumerateFileSystemEntries(directory))
        {
            string fileName = Path.GetFileName(fullPath);
            bool isDirectory = (File.GetAttributes(fullPath) & FileAttributes.Directory) == FileAttributes.Directory;
            yield return new(
                Title: fileName.Replace('\\', '/'),
                Label: fullPath.Replace('\\', '/'),
                BrowseIdentifier: isDirectory ? fullPath : null,
                ActionIdentifier: isDirectory ? null : fullPath,
                ThumbnailUri: $"{this._uriPrefix}{(isDirectory ? "folder" : "file")}.jpg"
            );
        }
    }

    private Task OnDirectoryAction(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        Console.WriteLine("DIRECTORY ACTION {0}:\"{1}\"", deviceId, actionIdentifier);
        return Task.CompletedTask;
    }
}
