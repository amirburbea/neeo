namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Parameters describing an info item dialog to add to a directory.
/// </summary>
/// <param name="Title">The text of the button to trigger the dialog.</param>
/// <param name="Text">The text of the info dialog.</param>
/// <param name="ActionIdentifier">Optional action identifier for the button.</param>
/// <param name="AffirmativeButtonText">Text for the "OK" button.</param>
/// <param name="NegativeButtonText">Text for the Cancel/Close button.</param>
public sealed record class DirectoryInfoItem(
    string Title,
    string Text,
    string? ActionIdentifier = null,
    string? AffirmativeButtonText = null,
    string? NegativeButtonText = null
);
